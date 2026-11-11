# Proposal

## Why

Drones and StudentGroups created by mistake (a typo in the Name, a test entry, a group that never borrowed anything) cannot be removed today, so they stay in every list and in the Loan form. REQ-011 and REQ-012 allow removing them, but only when doing so cannot touch the loan history that REQ-010 requires to be kept forever.

## What Changes

- A Drone can be deleted if it has no Loans at all, neither active nor returned (REQ-011).
- A StudentGroup can be deleted if it has no Loans at all, neither active nor returned (REQ-012).
- Deleting a Drone or StudentGroup that has at least one Loan is rejected with 409 Conflict; the Drone or StudentGroup and all its Loans and LoanItems stay stored unchanged.
- Deleting a Drone or StudentGroup that does not exist is rejected with 404 Not Found.
- New endpoints `DELETE /api/drones/{id}` and `DELETE /api/student-groups/{id}` (204 No Content on success, ProblemDetails on error).
- The Drones page and the Student Groups page get a "Delete" button in every table row. A successful delete removes the row and shows a confirmation; a rejected delete shows the backend message in an error alert and leaves the row in place.
- Loans themselves stay non-deletable (REQ-010 is unchanged).

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `drone-management`: adds the requirement "Delete Drone" (REQ-011).
- `student-group-management`: adds the requirement "Delete StudentGroup" (REQ-012).
- `web-ui`: adds the requirement "Delete from the list" for the Delete button, its feedback and keyboard use on the Drones and Student Groups pages.

## Impact

- Backend: `IDroneService`/`DroneService` and `IStudentGroupService`/`StudentGroupService` get a `DeleteAsync` method; `DronesController` and `StudentGroupsController` get a `DELETE` action. The existing `NotFoundException`/`ConflictException` and `DomainExceptionHandler` are reused. No database schema change and no migration: the Loan foreign keys already use `DeleteBehavior.Restrict`.
- Backend tests: new xUnit tests for every new Given/When/Then scenario, plus HTTP tests for 204, 404 and 409.
- Frontend: `DroneService` and `StudentGroupService` get a `delete(id)` method; the Drones and Student Groups pages get a Delete button per row with Vitest tests.
- API consumers: two new endpoints; existing endpoints are unchanged (no breaking change).
