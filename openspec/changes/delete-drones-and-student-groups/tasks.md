# Tasks

The backend scenarios (REQ-011, REQ-012) get xUnit tests. The `web-ui` scenarios (UI-008) get Vitest component tests, as in `improve-frontend-design`; the backend behavior behind them (204/409) is covered by the xUnit HTTP tests in 1.3.

## 1. Backend: delete Drone and StudentGroup

- [x] 1.1 Implement `DroneService.DeleteAsync` (404 for an unknown Id, 409 if any Loan references the Drone, `DbUpdateException` mapped to 409) and `DELETE /api/drones/{id:int}` returning 204 (design D1-D3), with xUnit tests for scenarios "Drone without Loans is deleted", "Drone with loan history", "Drone with an active Loan" and "Unknown Drone"; verify `dotnet test` passes (#69)
- [x] 1.2 Implement `StudentGroupService.DeleteAsync` and `DELETE /api/student-groups/{id:int}` the same way (design D1-D3), with xUnit tests for scenarios "StudentGroup without Loans is deleted", "StudentGroup with loan history", "StudentGroup with an active Loan" and "Unknown StudentGroup", and an `AppDbContextTests` case "StudentGroup_with_Loans_cannot_be_deleted" (design D5); verify `dotnet test` passes (#70)
- [x] 1.3 Add HTTP tests to `HttpStatusMappingTests` for `DELETE /api/drones/{id}`: 204 for a Drone without Loans, 404 for an unknown Id, and 409 with a ProblemDetails `detail` for a Drone with a Loan (design D3); verify `dotnet test` passes (#71)

## 2. Frontend: Delete buttons

- [x] 2.1 Add `delete(id)` to `DroneService` and `StudentGroupService` (`DELETE` to the new endpoints, `Observable<void>`), with Vitest tests using `HttpTestingController` that check the method and URL; verify `ng test --watch=false` passes (#72)
- [x] 2.2 Add the Delete column and button to the `/drones` page, with `aria-label` "Delete Drone {Name}", a `deletingId` signal, and the success and error alerts in the list card (design D4). Add Vitest tests for scenarios "Drone is deleted from the Drones page", "Rejected delete keeps the row", "Delete button names its row" and "Button is disabled while deleting"; verify `ng test --watch=false` passes (#73)
- [x] 2.3 Add the Delete column and button to the `/student-groups` page the same way, with `aria-label` "Delete StudentGroup {Name}" (design D4). Add Vitest tests for scenarios "StudentGroup is deleted from the Student Groups page" and "Rejected StudentGroup delete keeps the row"; verify `ng test --watch=false` passes (#74)

## 3. Integration

- [x] 3.1 With API and UI running, delete a Drone and a StudentGroup without Loans, and try to delete a Drone and a StudentGroup with a returned Loan. Check that the loan history on `/history` is unchanged and that the Delete buttons work with Tab and Enter at 1280px and 375px. Verify `ng build`, `ng test --watch=false` and `dotnet test` pass, and note the results in the PR (#75)

## Workflow follow-up

- Create GitHub issues from these tasks with `/openspec-to-github`.
- Implement one issue per branch/PR with `/openspec-apply-with-github`.
- Archive with `/openspec-archive-with-github` after all issues are merged.
