# Design

## Context

The repository contains only scaffolds: `backend/DroneInventory.Api` is the ASP.NET Core 10 Web API template (controllers enabled, `WeatherForecast` sample, OpenAPI, no EF Core, no test project); `frontend/drone-inventory-ui` is an Angular 21 standalone app with an empty route table, no `HttpClient` and the default placeholder page. The API runs on `http://localhost:5168`, the Angular dev server on `http://localhost:4200`. Motivation and scope: see proposal.md; behavior: see `specs/`.

## Goals / Non-Goals

**Goals:**
- A layered backend (Controller → Service → DbContext) where every business rule from the specs lives in a service and is covered by xUnit tests.
- A simple, consistent REST contract with clear error status codes the UI can display.
- Deterministic tests for date-dependent behavior (LoanDate, ReturnDate).

**Non-Goals:**
- Authentication/authorization, editing or deleting Drones/StudentGroups, pagination, UI end-to-end tests.
- Production deployment setup (the SQLite file lives next to the API).

## Decisions

### D1: Data model
```
Drone(Id int PK, Name string unique, Description string)
StudentGroup(Id int PK, Name string unique)
Loan(Id int PK, DroneId FK, StudentGroupId FK, LoanDate DateOnly, ReturnDate DateOnly?, Status enum Active|Returned)
LoanItem(Id int PK, LoanId FK, Type enum Battery|Propeller, Quantity int)
```
- Enums are stored as strings (readable DB, stable when enum order changes).
- `DateOnly` for LoanDate/ReturnDate because the specs only talk about dates. Alternative `DateTime` rejected: time zones/time of day would leak into "current date" comparisons.
- No availability column on Drone. Availability is computed in queries as `Loans.Any(l => l.Status == Active)`. Alternative (stored flag) rejected by project rule and risk of drift.
- No cascade delete from Drone/StudentGroup to Loan (`DeleteBehavior.Restrict`) and no delete endpoints for Loan, so history can't be lost.

### D2: Double-loan protection in two layers
The `LoanService` checks for an existing active Loan before inserting (gives a clear 409). Additionally a filtered unique index on `Loan(DroneId) WHERE Status = 'Active'` guarantees the rule even under concurrent requests; a `DbUpdateException` from that index is mapped to the same conflict. Alternative (service check only) rejected because two simultaneous requests could both pass the check.

### D3: Layering and dependency injection
- `AppDbContext` registered with `AddDbContext` (SQLite, connection string in `appsettings.json`: `Data Source=droneinventory.db`).
- Services `DroneService`, `StudentGroupService`, `LoanService` behind interfaces, registered as scoped. Controllers are thin: map DTOs, call service, translate results to HTTP.
- `TimeProvider` is injected into `LoanService` to get the current date; tests use a fake `TimeProvider` with a fixed date. Alternative (`DateTime.Today` directly) rejected: untestable for the 19.08.2026 scenario.
- Services signal rule violations via typed exceptions (`ValidationException`, `NotFoundException`, `ConflictException`) handled by one `IExceptionHandler` that produces `ProblemDetails`. Alternative (result objects) considered; exceptions keep the services short for this project size.

### D4: REST API
| Method | Route | Success | Errors |
|---|---|---|---|
| GET | `/api/drones` | 200 `[{id,name,description,status}]` | – |
| POST | `/api/drones` `{name,description}` | 201 drone | 400 empty name, 409 duplicate name |
| GET | `/api/student-groups` | 200 `[{id,name}]` | – |
| POST | `/api/student-groups` `{name}` | 201 group | 400 empty name, 409 duplicate name |
| POST | `/api/loans` `{droneId,studentGroupId,batteries,propellers}` | 201 loan | 400 negative quantity, 404 unknown drone/group, 409 drone already loaned |
| GET | `/api/loans/active` | 200 `[{id,studentGroupName,droneName,batteries,propellers,loanDate}]` | – |
| GET | `/api/loans/history` | 200 same + `returnDate` | – |
| POST | `/api/loans/{id}/return` | 200 loan | 404 unknown loan, 409 already returned |

Dates are serialized as ISO `yyyy-MM-dd`; the UI formats them as `dd.MM.yyyy`. Drone `status` is the string `"Available"` or `"Loaned"`. Return is a `POST` action rather than `PUT/PATCH` on status, so clients cannot set arbitrary states.

### D5: Database creation
EF Core migrations (`dotnet ef migrations add InitialCreate`) applied on startup with `Database.Migrate()` in Development. Alternative `EnsureCreated` rejected because it blocks later migrations.

### D6: Tests
New project `backend/DroneInventory.Api.Tests` (xUnit). Service tests run against SQLite **in-memory** (`DataSource=:memory:` with an open connection) so the real relational behavior, including the filtered unique index, is exercised. Alternative EF InMemory provider rejected: it ignores unique indexes and constraints. One test (or theory) per Given/When/Then scenario, named after the scenario. A few controller-level tests via `WebApplicationFactory` verify the HTTP status mapping.

### D7: Frontend
- `provideHttpClient()` in `app.config.ts`; API base URL in `src/environments`-style constant; CORS policy in the API allowing `http://localhost:4200`. Alternative Angular dev proxy considered; CORS is simpler to explain and also works for `ng build` output served elsewhere.
- One typed service per resource (`DroneService`, `StudentGroupService`, `LoanService`) and standalone routed pages: `/drones` (list + create form), `/student-groups` (list + create form), `/loans` (active list, loan form with drone/group selection and quantities, return button), `/history`.
- The UI performs no business validation beyond input types; it shows the backend's `ProblemDetails.detail`/`title` on errors.

## Risks / Trade-offs

- [Filtered index syntax is SQLite-specific] → Defined via `HasFilter` in the model config; acceptable since SQLite is the chosen DB.
- [Exceptions for control flow] → Limited to three well-known types and one handler; easy to replace with result types later.
- [Name uniqueness is case-sensitive in SQLite by default] → Names are trimmed; uniqueness compared case-insensitively via `COLLATE NOCASE` on the Name columns.
- [Current date depends on server time zone] → Single school location; `TimeProvider.System` local date is used.

## Migration Plan

Greenfield: no existing data. The SQLite file is created by the first migration on startup and is git-ignored. Rollback = delete the DB file.
