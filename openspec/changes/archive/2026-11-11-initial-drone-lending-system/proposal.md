# Proposal

## Why

Drones and their accessories are currently loaned to student groups using a paper list on the cabinet door. That list cannot reliably tell who has which drone, does not prevent a drone from being handed out twice, and loses history when it is replaced. The project scaffold (ASP.NET Core 10 API + Angular UI) exists but contains no domain functionality yet, so this change delivers the first usable version of the Drone Lending System as described in `docs/requirements.md` (REQ-001 to REQ-010).

## What Changes

- Introduce the domain model **Drone**, **StudentGroup**, **Loan** and **LoanItem**, persisted with Entity Framework Core in SQLite.
- Allow creating and listing drones; each drone's availability (Available | Loaned) is derived from its active Loan, never stored.
- Allow creating and listing student groups.
- Allow loaning an available drone to a student group together with battery and propeller quantities (integers >= 0); reject a second active Loan for the same drone.
- Allow listing active loans (group, drone, batteries, propellers, loan date).
- Allow returning an active loan: the Loan becomes Returned, ReturnDate is set to the current date and the drone becomes available again; returning twice is rejected.
- Keep returned loans permanently and display them as loan history.
- Provide REST/JSON endpoints in the backend for all of the above; all business rules and validation live in the backend.
- Provide Angular pages for drones, student groups, active loans and loan history that consume only the REST API.
- Add an xUnit test project covering every Given/When/Then scenario.
- Remove the template `WeatherForecast` example from the backend and the placeholder page from the frontend.

Assumptions (minor, recorded here rather than asked):
- Drone names and student group names are unique (the requirements phrase scenarios as "GIVEN no drone named ... exists"); creating a duplicate name is rejected.
- Listing student groups is added because the loan form needs to select an existing group (not explicitly in REQ-001..010).
- LoanDate and ReturnDate are calendar dates (no time of day); the UI displays them as `dd.MM.yyyy`.
- A Loan always contains one LoanItem per Type (Battery, Propeller); a quantity of 0 is valid.

## Capabilities

### New Capabilities
- `drone-management`: creating drones and listing them with their derived availability status (REQ-001, REQ-002).
- `student-group-management`: creating and listing student groups (REQ-003).
- `drone-lending`: loaning drones with accessory quantities, preventing double loans, listing active loans, returning loans and keeping the loan history (REQ-004 to REQ-010).

### Modified Capabilities
- None (no existing specs).

## Impact

- **Backend** (`backend/DroneInventory.Api`): new entities, `DbContext`, EF Core migrations, services with business rules, controllers, DTOs; new NuGet dependencies `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design`; CORS configuration for the Angular dev server. The `WeatherForecast` sample is removed.
- **Tests**: new xUnit project `backend/DroneInventory.Api.Tests`.
- **Frontend** (`frontend/drone-inventory-ui`): `HttpClient` setup, API services, routed pages for drones, student groups, active loans and loan history; placeholder template replaced.
- **Data**: a new SQLite database file (not committed).
- **API**: new REST endpoints under `/api/drones`, `/api/student-groups`, `/api/loans`.
