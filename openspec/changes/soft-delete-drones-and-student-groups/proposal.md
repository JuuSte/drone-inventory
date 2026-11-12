# Proposal

## Why

Today a Drone or StudentGroup can be deleted only if it never had a Loan. A Drone that is broken or retired, or a StudentGroup that has left, therefore stays in every list and in the Loan form forever. Its returned Loans are exactly what blocks the delete. The revised REQ-011 and REQ-012 in `docs/requirements.md` allow deleting anything without an active Loan, as long as the loan history (REQ-010) still shows the deleted Drone and StudentGroup. This is only possible if delete hides the entry instead of removing it, i.e. a soft delete.

## What Changes

- A Drone can be deleted if it has no active Loan, even if it has returned Loans (REQ-011). Deleting a Drone with an active Loan is still rejected with 409 Conflict.
- A StudentGroup can be deleted if it has no active Loan, even if it has returned Loans (REQ-012). Deleting a StudentGroup with an active Loan is still rejected with 409 Conflict.
- A deleted Drone or StudentGroup is no longer listed, cannot be used for a new Loan, and cannot be deleted again (404 Not Found).
- The loan history keeps showing the Names of deleted Drones and StudentGroups. Loans and LoanItems are never changed by a delete.
- A Name SHALL be unique only among Drones (or StudentGroups) that are not deleted, so a deleted Name can be used again.
- **Behavior change**: `DELETE /api/drones/{id}` and `DELETE /api/student-groups/{id}` now return 204 for an entry with only returned Loans, where they used to return 409. The endpoints and response shapes are unchanged.
- The frontend Delete buttons keep their behavior. Only the rejected-delete case narrows to "has an active Loan".

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `drone-management`: "Delete Drone" now allows Drones with only returned Loans to be deleted and defines what a deleted Drone means. "Drone Name is unique" now applies only to Drones that are not deleted.
- `student-group-management`: "Delete StudentGroup" and "StudentGroup Name is unique" change the same way.
- `web-ui`: in "Delete from the list", the rejected-delete scenarios now describe an active Loan, and a scenario is added for deleting a Drone with only returned Loans.

## Impact

- Backend models and database: `Drone` and `StudentGroup` get an `IsDeleted` flag. The unique Name indexes become filtered indexes on entries that are not deleted. A new EF Core migration is required.
- Backend services: `DroneService` and `StudentGroupService` change `DeleteAsync`, `GetAllAsync` and the duplicate-Name check. `LoanService.CreateAsync` rejects deleted Drones and StudentGroups. `LoanService` history and active-Loan queries stay as they are.
- Backend tests: the existing delete tests (`DroneServiceDeleteTests`, `StudentGroupServiceDeleteTests`, `HttpStatusMappingTests`) are rewritten for the new rule. New tests cover the loan history, Loan creation and Name reuse.
- Frontend: no production code change. Vitest scenario names and fixtures for the rejected delete are updated.
- `docs/requirements.md` already contains the new REQ-011 and REQ-012.
