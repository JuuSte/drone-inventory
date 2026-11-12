# Design

## Context

See proposal.md - Why. The requirements are in `specs/drone-management` (REQ-011), `specs/student-group-management` (REQ-012) and `specs/web-ui` (UI-008).

Current state that shapes the approach:
- `DroneService.DeleteAsync` and `StudentGroupService.DeleteAsync` hard-delete the entity. They throw `ConflictException` if any Loan references it. As a backstop, they map the `DbUpdateException` from the `Restrict` foreign key to the same conflict, which is tested with `InsertCompetingLoanInterceptor`.
- `LoanService.GetHistoryAsync` and `GetActiveAsync` project `l.Drone.Name` and `l.StudentGroup.Name` through required navigations, so the generated SQL uses INNER JOINs.
- `Drones.Name` and `StudentGroups.Name` have a unique index with `NOCASE` collation. `AppDbContext` already uses a filtered unique index for one active Loan per Drone (`HasFilter`).
- `Program` runs `Database.Migrate()` in Development. The tests use `EnsureCreated()`, so they build the schema from the model and never run migration SQL.
- The frontend fills the Loan form from `GET /api/drones` (filtered to "Available") and `GET /api/student-groups`. If a deleted entry is left out of these lists, it disappears from the form with no frontend change.

## Goals / Non-Goals

**Goals:**
- A deleted Drone or StudentGroup cannot end up with an active Loan, even when a delete and a Loan creation run at the same time.
- Loan history and active Loans are queried exactly as today and still show deleted Names.
- Keep the existing exceptions, status codes, endpoints and DTOs.

**Non-Goals:**
- Restoring (undeleting) a deleted Drone or StudentGroup, or listing deleted entries.
- Hard delete, including for entries without Loans.
- Exposing the deleted flag in any DTO or showing "deleted" in the history.
- Deleting Loans or LoanItems (REQ-010 is unchanged).

## Decisions

### D1: A boolean `IsDeleted` column on Drone and StudentGroup
Both entities get `public bool IsDeleted { get; set; }`, which defaults to `false` and is required. It is a technical flag, not a domain field, so it is not part of any DTO.
- *Alternative: `DeletedAt` timestamp.* Rejected for now, because nothing reads the date. It can be added later without a behavior change.
- *Alternative: move deleted entries to an archive table.* Rejected, because the Loans would then need a second foreign key target.

### D2: Filter explicitly in the services, not with a global query filter
`DroneService.GetAllAsync`, `StudentGroupService.GetAllAsync`, the duplicate-Name checks, `DeleteAsync` and `LoanService.CreateAsync` add `!IsDeleted` themselves. The Loan queries stay unchanged.
- *Alternative: `HasQueryFilter(d => !d.IsDeleted)`.* Rejected. A filter on the principal of a required navigation makes EF Core drop the Loans of deleted entries from INNER JOIN projections, so the loan history would silently lose rows. That is exactly what REQ-011 and REQ-012 forbid. EF also warns about this configuration. Working around it with `IgnoreQueryFilters()` in every Loan query is easier to forget than adding `!IsDeleted` in the few places that need it.

### D3: Delete is one conditional UPDATE
`DeleteAsync(id)` runs:
```
context.Drones
    .Where(d => d.Id == id && !d.IsDeleted && !d.Loans.Any(l => l.Status == LoanStatus.Active))
    .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsDeleted, true))
```
If one row is affected, the delete succeeded. If none is, the service reads the Drone again to choose the error:
- the Drone is missing or `IsDeleted` → `NotFoundException` "Drone with Id {id} was not found."
- otherwise it has an active Loan → `ConflictException` `Drone "TELLO-07" cannot be deleted because it has an active Loan.`

StudentGroup works the same way with `StudentGroupId`. The check and the write happen in one SQL statement, so no Loan can slip in between them. This replaces the `DbUpdateException` backstop, which no longer applies because an UPDATE never violates the `Restrict` foreign key.

### D4: Loan creation locks the Drone and StudentGroup rows
A race in the other direction remains. `CreateAsync` checks the Drone, a delete commits, and then the Loan is inserted. To close it, `LoanService.CreateAsync` runs in a transaction. Its first statements are no-op UPDATEs: `UPDATE Drones SET IsDeleted = IsDeleted WHERE Id = @id AND IsDeleted = 0` (via `ExecuteUpdateAsync`), and the same for StudentGroups. If either affects 0 rows, the request gets the existing 404 message. Otherwise SQLite now holds the write lock until the Loan is committed, so a concurrent delete waits and then sees the active Loan (D3).
- *Alternative: SQLite trigger `BEFORE INSERT ON Loans`.* Rejected, because the tests build the schema with `EnsureCreated()` and would never run the trigger.
- *Alternative: accept the race.* Rejected, because the archived delete change explicitly protected against concurrent requests, and this keeps that guarantee.

### D5: Filtered unique Name indexes
`HasIndex(d => d.Name).IsUnique().HasFilter("\"IsDeleted\" = 0")`, and the same for StudentGroup, keeps `NOCASE`. The duplicate-Name check in `CreateAsync` gets `&& !d.IsDeleted`, and the `DbUpdateException` mapping stays. This follows the user's decision that a deleted Name can be used again.

### D6: Migration `SoftDeleteDronesAndStudentGroups`
Generated with `dotnet ef migrations add`. It adds the `IsDeleted INTEGER NOT NULL DEFAULT 0` columns and drops and recreates the two Name indexes with their filter. Existing rows become "not deleted". No data is transformed.

### D7: Messages and HTTP
The endpoints, 204, 404, 409 and ProblemDetails are unchanged (archived design D3). The only change is the 409 text: "...because it has an active Loan." The frontend already shows the backend message as is.

### D8: Tests
- **Service tests** (xUnit, `SqliteTestDatabase`): `DroneServiceDeleteTests` and `StudentGroupServiceDeleteTests` are rewritten with one test per scenario in the delta specs. The history scenarios call `LoanService.GetHistoryAsync`, and the "cannot be loaned" scenarios call `LoanService.CreateAsync`. The old concurrency test and `InsertCompetingLoanInterceptor` are removed: D3 has no gap between check and write that a test could hit, and the "Drone with an active Loan" scenario covers the condition.
- **Name reuse** scenarios go in `DroneServiceCreateTests` and `StudentGroupServiceCreateTests`.
- **D4**: `LoanServiceCreateTests` gets a test that a Loan for a deleted Drone or StudentGroup is rejected as not found and nothing is stored.
- **HTTP** (`HttpStatusMappingTests`): 204 for a Drone with a returned Loan, 409 for an active Loan with the new detail text, and 404 for a Drone that is already deleted.
- **Loan lists after delete** (HTTP, `LoanListsAfterDeleteTests`): `GET /api/loans/history` returns the Loan of a deleted Drone and a deleted StudentGroup with both Names, end to end through the real query and JSON. A guard test does the same for `GET /api/loans/active`, with the deleted flags set directly in the database. That state cannot be reached through the API, but the test fails if someone later adds a global query filter, which would silently drop rows (D2).
- `AppDbContextTests.Drone_with_Loans_cannot_be_deleted` stays. The `Restrict` foreign key still protects history against a hard delete.
- **Vitest**: the rejected-delete tests on the Drones and Student Groups pages get the new message and active-Loan wording. A test for "Drone with only returned Loans is deleted from the Drones page" covers the row removal (frontend side only).

## Risks / Trade-offs

- [A new query on Drones or StudentGroups forgets `!IsDeleted`] → The specs' "no longer listed" and "cannot be loaned" scenarios are tested on every public entry point. A code comment on `IsDeleted` points to D2.
- [Two Drones with the same Name in the history after reuse] → This was accepted with the Name-reuse decision. Each history row is still a distinct Loan with its own dates.
- [The D4 lock makes Loan creation take the write lock earlier] → SQLite allows only one writer anyway, and the transaction contains just three short statements.
- [An accidental delete cannot be undone in the UI] → The history is kept, and the entry can be re-created under the same Name (D5). Undelete is a non-goal.

## Migration Plan

1. Deploy the backend. In Development, `Database.Migrate()` applies `SoftDeleteDronesAndStudentGroups` on startup. In other environments, run `dotnet ef database update`.
2. The frontend needs no deploy. The old frontend works unchanged with the new backend.
3. Rollback: revert the migration (`dotnet ef database update InitialCreate`) and redeploy the previous backend. This loses the deleted flags, so soft-deleted entries reappear. No Loan data is affected. If a deleted Name was already reused, recreating the plain unique index fails; such duplicates must be renamed by hand before rolling back.
