# Tasks

## 1. Backend setup

- [x] 1.1 Remove the `WeatherForecast` sample, add `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design`, add CORS for `http://localhost:4200` and a `ProblemDetails` exception handler for `ValidationException` (400), `NotFoundException` (404) and `ConflictException` (409) (design D3); verify `dotnet build` succeeds and `*.db` is git-ignored (#1)
- [x] 1.2 Create the xUnit test project `backend/DroneInventory.Api.Tests` referencing the API, with a test helper that builds an `AppDbContext` on SQLite in-memory and a fake `TimeProvider` (design D6); verify `dotnet test` runs with a passing smoke test (#2)

## 2. Backend: data model

- [x] 2.1 Add entities `Drone`, `StudentGroup`, `Loan`, `LoanItem`, enums `LoanStatus` and `LoanItemType` (stored as strings), `AppDbContext` with unique `NOCASE` Name indexes, the filtered unique index on active Loans per Drone and `Restrict` delete behavior (design D1, D2); create the `InitialCreate` migration applied on startup; verify `dotnet ef database update` creates the tables and a test confirms a second active Loan for the same Drone is refused by the database (#3)

## 3. Backend: drone-management

- [x] 3.1 Implement `DroneService.Create` and `POST /api/drones` (trimmed name required, unique) with xUnit tests for scenarios "Drone is created", "Name is required" and "Duplicate Name"; verify the tests pass (#4)
- [x] 3.2 Implement `DroneService.GetAll` and `GET /api/drones` with status derived from the active Loan, with xUnit tests for scenarios "Status derived from active Loan" and "Drone with only returned Loans is available"; verify the tests pass (#5)

## 4. Backend: student-group-management

- [x] 4.1 Implement `StudentGroupService.Create` and `POST /api/student-groups` with xUnit tests for scenarios "StudentGroup is created", "Name is required" and "Duplicate Name"; verify the tests pass (#6)
- [x] 4.2 Implement `StudentGroupService.GetAll` and `GET /api/student-groups` with an xUnit test for scenario "StudentGroups are listed"; verify the test passes (#7)

## 5. Backend: drone-lending

- [ ] 5.1 Implement `LoanService.Create` and `POST /api/loans` (LoanDate from `TimeProvider`, Status Active) with xUnit tests for scenarios "Available Drone is loaned" and "Unknown Drone or StudentGroup"; verify the tests pass (#8)
- [ ] 5.2 Reject a Loan for a Drone with an active Loan (409, including mapping the index `DbUpdateException`) with an xUnit test for scenario "Drone is already loaned"; verify the test passes (#9)
- [ ] 5.3 Create Battery and Propeller LoanItems and reject negative quantities (400) with xUnit tests for scenarios "Loan with batteries and propellers" and "Negative quantity"; verify the tests pass (#10)
- [ ] 5.4 Implement `GET /api/loans/active` with xUnit tests for scenarios "Active Loan is listed" (fixed date 19.08.2026) and "Returned Loan is not listed as active"; verify the tests pass (#11)
- [ ] 5.5 Implement `POST /api/loans/{id}/return` setting Status Returned and ReturnDate from `TimeProvider` with xUnit tests for scenarios "Active Loan is returned", "Return twice", "Unknown Loan" and "ReturnDate is set"; verify the tests pass (#12)
- [ ] 5.6 Implement `GET /api/loans/history` with xUnit tests for scenarios "Returned Loan appears in history" and "Loan is never deleted"; verify the tests pass (#13)
- [ ] 5.7 Add `WebApplicationFactory` tests checking the HTTP status mapping (201, 400, 404, 409) for one endpoint per error type; verify the tests pass (#14)

## 6. Frontend setup

- [x] 6.1 Replace the placeholder page with a layout and navigation (Drones, Student Groups, Loans, History), add `provideHttpClient()`, the API base URL constant and routes; verify `ng build` succeeds and navigation works in the browser
- [x] 6.2 Add typed models and API services for Drone, StudentGroup and Loan with an error helper that extracts the `ProblemDetails` message; verify with Vitest unit tests using `HttpTestingController` (#16)

## 7. Frontend: pages

- [x] 7.1 Implement the `/drones` page: list with Name, status and Description plus a create form showing backend errors; verify manually that creating "TELLO-07" shows it as "Available" and an empty name shows the error (#17)
- [x] 7.2 Implement the `/student-groups` page: list plus create form showing backend errors; verify manually that "Group 3" can be created and a duplicate is rejected (#18)
- [x] 7.3 Implement the loan form on `/loans` (select available Drone and StudentGroup, battery and propeller quantities); verify manually that loaning "TELLO-07" to "Group 3" with 4/8 succeeds and a second Loan of "TELLO-07" shows the error (#19)
- [x] 7.4 Implement the active Loans table on `/loans` (Group | Drone | Batteries | Propellers | LoanDate as `dd.MM.yyyy`) with a Return button; verify manually that returning removes the row and the Drone shows "Available" again (#20)
- [ ] 7.5 Implement the `/history` page listing returned Loans with LoanDate, ReturnDate and accessories; verify manually that a returned Loan appears there (#21)

## 8. Integration

- [ ] 8.1 Run the full flow end to end (create Drone and StudentGroup, loan, list, return, history) against a fresh database with API and UI running; verify all `dotnet test` and `ng test` suites pass and document start commands in the root README (#22)

## Workflow follow-up

- Create GitHub issues from these tasks with `/openspec-to-github`.
- Implement one issue per branch/PR with `/openspec-apply-with-github`.
- Archive with `/openspec-archive-with-github` after all issues are merged.
