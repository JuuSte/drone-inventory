# Design

## Context

See proposal.md - Why. The requirements are in `specs/drone-management`, `specs/student-group-management` (REQ-011, REQ-012) and `specs/web-ui` (UI-008).

Current state that shapes the approach:
- Services (`DroneService`, `StudentGroupService`) hold the business rules and throw `ValidationException`, `NotFoundException` and `ConflictException`. `DomainExceptionHandler` maps these to 400, 404 and 409 ProblemDetails.
- In `AppDbContext`, both Loan foreign keys (`DroneId`, `StudentGroupId`) and the LoanItem foreign key already use `DeleteBehavior.Restrict`. `AppDbContextTests.Drone_with_Loans_cannot_be_deleted` shows that SQLite already refuses to delete a Drone that has Loans.
- The Loans page already has a row action pattern: a `button--secondary` "Return Loan" button with an `aria-label` naming the row, a `returningId` signal that disables the busy row's button, and separate success and error alerts in the list card above the table.

## Goals / Non-Goals

**Goals:**
- Deleting can never remove or change a Loan or LoanItem, even when two requests run at the same time.
- Reuse the existing exception, ProblemDetails and row action patterns; add no new mechanism.

**Non-Goals:**
- Deleting Loans or LoanItems (REQ-010 stays unchanged).
- Soft delete, undo or a recycle bin.
- Editing Drones or StudentGroups.
- Telling the frontend in advance whether a Drone or StudentGroup can be deleted (no new DTO field).

## Decisions

### D1: The rule lives in the service, checked against all Loans
`DeleteAsync(int id)` in `DroneService` and `StudentGroupService` does three things:
1. It loads the entity, or throws `NotFoundException` with "Drone with Id {id} was not found." (the same wording as `LoanService`).
2. If any Loan references it (`context.Loans.AnyAsync(l => l.DroneId == id)`), regardless of Status, it throws `ConflictException`. The message is `Drone "TELLO-07" cannot be deleted because it has Loans.` (StudentGroup is worded the same way.)
3. Otherwise it removes the entity and saves.

The check covers active and returned Loans alike, because a returned Loan is loan history.
- *Alternative: cascade delete.* Rejected, because it would delete history (REQ-010).
- *Alternative: block only active Loans and set the foreign key to null on returned ones.* Rejected, because the history would lose its Drone or StudentGroup Name.

### D2: The database is the second safety layer
A Loan could be created between the check in D1 and `SaveChangesAsync`. The existing `Restrict` foreign keys then make SQLite reject the delete. `DeleteAsync` catches `DbUpdateException` and throws the same `ConflictException`. This mirrors the duplicate-Name handling in `CreateAsync` and the two-layer double-loan protection from the initial change. No schema change or migration is needed.

### D3: REST endpoints
- `DELETE /api/drones/{id:int}` and `DELETE /api/student-groups/{id:int}`.
- **204** No Content on success. **404** if the entity does not exist. **409** if it has Loans. Errors are ProblemDetails through the existing `DomainExceptionHandler`.
- Each controller action gets `ProducesResponseType` attributes for 204, 404 and 409, like the existing actions.

### D4: Frontend row action
- `DroneService.delete(id)` and `StudentGroupService.delete(id)` call the new endpoints and return `Observable<void>`.
- Each table row gets a `button--secondary` button labeled "Delete" in a new last column, with `aria-label="Delete Drone {Name}"` or `"Delete StudentGroup {Name}"`.
  - There is no `confirm()` dialog, which the frontend-design rule forbids. Only Drones and StudentGroups without any Loan can be deleted, and re-creating one is quick.
  - The button stays neutral rather than `--danger`, because `--danger` is reserved for errors.
- A `deletingId` signal disables the busy row's button.
- On success the row is removed from the signal and the list card shows a confirmation, e.g. `Drone "TELLO-08" deleted.`
- On error the row stays and the list card shows `problemDetailsMessage(error)` in an `alert--error`.
- These delete alerts live in the list card above the table, like the return alerts on the Loans page. Starting a delete clears all of the page's messages, including the create form's.
- The button is shown on every row. The backend decides, so the frontend never needs to know about returned Loans. The `colspan` of the empty-state row grows by one.

### D5: Tests
- **xUnit service tests**, one per scenario: `DroneServiceDeleteTests` and `StudentGroupServiceDeleteTests`. They cover no Loans → removed and no longer listed; returned Loan → conflict and still stored; active Loan → conflict and still loaned or listed; unknown Id → not found.
- **D2 backstop:** the existing `AppDbContextTests.Drone_with_Loans_cannot_be_deleted` already covers the Drone side. A new `StudentGroup_with_Loans_cannot_be_deleted` case mirrors it for StudentGroups. The `catch` in `DeleteAsync` only maps that exception and needs no extra race test.
- **HTTP tests** in `HttpStatusMappingTests`: 204, 404 and 409 for `DELETE /api/drones/{id}`.
- **Vitest:** service tests for the DELETE requests, and page tests for the UI-008 scenarios.

## Risks / Trade-offs

- [A wrong row is deleted by a mis-click, with no confirmation] → Only entries without any Loan can be deleted, so no history is affected, and the entry can be re-created at once. The confirmation names what was deleted.
- [A Drone or StudentGroup is deleted in one browser tab while another tab still shows it in the Loan form] → Creating a Loan for it returns the existing 404 "not found" message, which the Loans page already shows.
- [Microsoft.Data.Sqlite foreign key enforcement turned off by a connection string] → It is on by default and the `AppDbContextTests` cases fail if it is turned off.

## Migration Plan

No database migration. The backend and frontend can be deployed together; the old frontend keeps working with the new backend because no existing endpoint changes. Rollback means redeploying the previous version.
