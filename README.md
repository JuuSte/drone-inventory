# Drone Inventory

Lending system for school drones: an ASP.NET Core 10 Web API with Entity Framework Core and
SQLite, plus an Angular 21 single-page UI.

- **Drone**: a loanable aircraft with a name, a description and a derived status (`Available` | `Loaned`).
- **StudentGroup**: a group of students that loans drones.
- **Loan**: the loan of one Drone to one StudentGroup, with `LoanDate`, optional `ReturnDate` and `Status` (`Active` | `Returned`).
- **LoanItem**: the battery and propeller quantities of a Loan.

The UI never touches the database; all rules (one active Loan per Drone, required names, non-negative
quantities, permanent history) live in the API.

## Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download) (`dotnet --version` → `10.x`)
- [Node.js](https://nodejs.org/) 20 or newer with npm (`node --version`)

Angular CLI is used through `npx`, so a global install is not needed.

## Project structure

```
backend/DroneInventory.Api        ASP.NET Core Web API (Controllers → Services → EF Core → SQLite)
backend/DroneInventory.Api.Tests  xUnit tests (service tests + HTTP status mapping)
frontend/drone-inventory-ui       Angular UI (standalone components, routed pages)
openspec/                         OpenSpec change with specs, design and tasks
docs/requirements.md              Requirements REQ-001 … REQ-010
```

## Setup

Install the backend dependencies (this also builds the projects):

```bash
cd backend/DroneInventory.Api
dotnet restore
```

Install the frontend dependencies:

```bash
cd frontend/drone-inventory-ui
npm install
```

## Start the API

From `backend/DroneInventory.Api`:

```bash
dotnet run --launch-profile http
```

The API listens on **http://localhost:5168**. In the Development environment it applies the EF Core
migrations on startup, so the SQLite database is created automatically.

## Start the UI

In a second terminal, from `frontend/drone-inventory-ui`:

```bash
npm start
```

This runs `ng serve`; the UI is available on **http://localhost:4200**. Start the API first, because
the UI calls it directly at `http://localhost:5168/api` (the API's CORS policy allows
`http://localhost:4200`).

| Page | Route | What it does |
|---|---|---|
| Drones | `/drones` | List Drones with status and create a Drone |
| Student Groups | `/student-groups` | List StudentGroups and create one |
| Loans | `/loans` | Loan an available Drone, list active Loans, return a Loan |
| History | `/history` | Returned Loans with LoanDate and ReturnDate |

## Database

The database is a SQLite file `droneinventory.db` next to the API project
(`backend/DroneInventory.Api/`). It is git-ignored and created by the migrations on the first start.

To start again with an **empty** database, stop the API and delete the file, then start it again:

```bash
rm -f backend/DroneInventory.Api/droneinventory.db*
```

## Run the tests

Backend (xUnit):

```bash
dotnet test backend/DroneInventory.Api.Tests/DroneInventory.Api.Tests.csproj
```

Frontend (Vitest through the Angular unit-test builder):

```bash
cd frontend/drone-inventory-ui
npx ng test --watch=false
```

## API overview

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/drones` | All Drones with their derived status |
| `POST` | `/api/drones` | Create a Drone (`name`, `description`) |
| `GET` | `/api/student-groups` | All StudentGroups |
| `POST` | `/api/student-groups` | Create a StudentGroup (`name`) |
| `POST` | `/api/loans` | Loan a Drone (`droneId`, `studentGroupId`, `batteries`, `propellers`) |
| `GET` | `/api/loans/active` | Active Loans |
| `GET` | `/api/loans/history` | Returned Loans |
| `POST` | `/api/loans/{id}/return` | Return an active Loan |

Errors are returned as `ProblemDetails`: `400` for validation, `404` for unknown Drone/StudentGroup/Loan
and `409` for duplicate names, a Drone that is already loaned or a Loan that is already returned.

## Troubleshooting

If restore or build fails with `NETSDK1226: Prune Package data not found .NETCoreApp 10.0
Microsoft.AspNetCore.App`, the installed .NET SDK is missing the ASP.NET Core reference pack data.
Add the property to the failing command:

```bash
dotnet test backend/DroneInventory.Api.Tests/DroneInventory.Api.Tests.csproj --property:AllowMissingPrunePackageData=true
dotnet run --launch-profile http --property:AllowMissingPrunePackageData=true
```
