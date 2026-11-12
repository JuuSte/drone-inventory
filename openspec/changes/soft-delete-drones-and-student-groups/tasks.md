# Tasks

The backend scenarios (REQ-011, REQ-012) get xUnit tests. The `web-ui` scenarios (UI-008) get Vitest component tests, as in the archived `delete-drones-and-student-groups`; the backend behavior behind them is covered by the xUnit tests in group 1.

## 1. Backend: soft delete

- [ ] 1.1 Add `IsDeleted` to `Drone` and `StudentGroup`, make the unique Name indexes filtered on `"IsDeleted" = 0` (design D1, D5), and generate the migration `SoftDeleteDronesAndStudentGroups` (design D6). Add `AppDbContextTests` cases: two Drones (and two StudentGroups) with the same Name can be stored if one is deleted, but not if both are not deleted. Verify `dotnet test` passes and `dotnet ef migrations list` shows the new migration
- [ ] 1.2 Rewrite `DroneService.DeleteAsync` as the conditional UPDATE from design D3 and filter `GetAllAsync` and the duplicate-Name check with `!IsDeleted` (design D2, D5). Rewrite `DroneServiceDeleteTests` with xUnit tests for scenarios "Drone with loan history", "Drone without Loans is deleted", "Drone with an active Loan", "Unknown Drone" and "Drone is deleted twice", and remove the interceptor test (design D8). Add "Name of a deleted Drone is reused" to `DroneServiceCreateTests`. Verify `dotnet test` passes
- [ ] 1.3 Do the same for `StudentGroupService` (design D2, D3, D5). Rewrite `StudentGroupServiceDeleteTests` with xUnit tests for scenarios "StudentGroup with loan history", "StudentGroup without Loans is deleted", "StudentGroup with an active Loan", "Unknown StudentGroup" and "StudentGroup is deleted twice". Add "Name of a deleted StudentGroup is reused" to `StudentGroupServiceCreateTests`. Verify `dotnet test` passes
- [ ] 1.4 Make `LoanService.CreateAsync` reject deleted Drones and StudentGroups as not found, using the transaction and no-op UPDATE lock from design D4. Add xUnit tests for scenarios "Deleted Drone cannot be loaned" and "Deleted StudentGroup cannot be selected for a Loan" (no Loan stored, the Drone stays "Available"), and check that the existing `LoanServiceCreateTests` and `LoanServiceNoDoubleLoanTests` still pass. Verify `dotnet test` passes
- [ ] 1.5 Update `HttpStatusMappingTests` for `DELETE /api/drones/{id}`: 204 for a Drone with a returned Loan, 409 with detail `Drone "TELLO-07" cannot be deleted because it has an active Loan.` for an active Loan, and 404 for a Drone that is already deleted (design D7). Verify `dotnet test` passes
- [ ] 1.6 Add xUnit HTTP tests (`Http/LoanListsAfterDeleteTests.cs`, using `DroneInventoryApiFactory`) for the "loan history still shows ..." part of scenarios "Drone with loan history" and "StudentGroup with loan history". Through the API, create the Drone "TELLO-07" and the StudentGroup "Group 3", create a Loan and return it, then delete both (`DELETE` returns 204). `GET /api/loans/history` must still return that Loan with `droneName` "TELLO-07", `studentGroupName` "Group 3" and its quantities, while `GET /api/drones` and `GET /api/student-groups` no longer list them. Add a guard test for the active Loans list (design D2): mark the Drone and StudentGroup of an active Loan as `IsDeleted` directly in the test database, then check that `GET /api/loans/active` still returns that Loan with both Names. Through the API this state cannot occur, because the delete is rejected. Verify `dotnet test` passes

## 2. Frontend: Delete button tests

- [ ] 2.1 Update the Vitest tests on the `/drones` page. "Rejected delete keeps the row" now uses an active Loan and the new backend message. Add "Drone with only returned Loans is deleted from the Drones page", which checks that the row is removed after a 204 (design D8). No production code change. Verify `ng test --watch=false` passes
- [ ] 2.2 Update the Vitest test "Rejected StudentGroup delete keeps the row" on the `/student-groups` page with an active Loan and the new backend message. No production code change. Verify `ng test --watch=false` passes

## 3. Integration

- [ ] 3.1 With API and UI running on a database migrated from the previous version, do three things. First, delete a Drone and a StudentGroup that have only returned Loans: check they disappear from their pages and from the Loan form, and that `/history` still shows their Loans with their Names. Second, try to delete a Drone with an active Loan and check that the error alert appears. Third, create a new Drone with the deleted Name. Verify `ng build`, `ng test --watch=false` and `dotnet test` pass, and note the results in the PR

## Workflow follow-up

- Create GitHub issues from these tasks with `/openspec-to-github`.
- Implement one issue per branch/PR with `/openspec-apply-with-github`.
- Archive with `/openspec-archive-with-github` after all issues are merged.
