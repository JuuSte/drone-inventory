# Drone Inventory - Claude Code Instructions

## Architecture
Angular Frontend (frontend/drone-inventory-ui)
      |  REST/JSON
      v
ASP.NET Core 10 Web API (backend/DroneInventory.Api)
      |
      v
Entity Framework Core -> SQLite

## Rules
- Business logic and validation belong in the backend.
- The frontend must never access the database directly.
- OpenSpec specifications (openspec/) are the source of truth for requirements.
- Use the domain terms Drone, StudentGroup, Loan, LoanItem everywhere.
- Drone availability is derived from the active Loan, not stored.
- Use dependency injection in the backend.
- Implement only the task you were asked for - never several tasks at once.
- Every Given/When/Then scenario needs a test (xUnit in the backend).

## Project Management
- Use GitHub Issues and the GitHub Project "Drone Inventory Project" (owner JuuSte) for all work.
- Use the GitHub CLI `gh` for issues and project updates.
- One issue = one branch (feature/<nr>-<name>) = one pull request.
- Skills: /openspec-to-github, /openspec-apply-with-github, /openspec-archive-with-github
