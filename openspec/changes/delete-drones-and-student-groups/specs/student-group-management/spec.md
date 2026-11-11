# Spec Delta

## ADDED Requirements

### Requirement: Delete StudentGroup
The system SHALL delete a StudentGroup only if it has no Loans, neither active nor returned. Deleting a StudentGroup that has at least one Loan SHALL be rejected as a conflict, and the StudentGroup and all its Loans SHALL remain stored unchanged, so loan history is never lost (REQ-010). Deleting a StudentGroup that does not exist SHALL be rejected as not found. (REQ-012)

#### Scenario: StudentGroup without Loans is deleted
- **GIVEN** the StudentGroup "Group 5" has no Loans
- **WHEN** "Group 5" is deleted
- **THEN** the StudentGroup is removed
- **AND** "Group 5" is no longer listed

#### Scenario: StudentGroup with loan history
- **GIVEN** the StudentGroup "Group 3" has a Loan with Status "Returned"
- **WHEN** "Group 3" is deleted
- **THEN** the request is rejected as a conflict
- **AND** the StudentGroup "Group 3" and its Loans remain stored

#### Scenario: StudentGroup with an active Loan
- **GIVEN** the StudentGroup "Group 3" has an active Loan
- **WHEN** "Group 3" is deleted
- **THEN** the request is rejected as a conflict
- **AND** the active Loan of "Group 3" is still listed

#### Scenario: Unknown StudentGroup
- **GIVEN** no StudentGroup with the requested Id exists
- **WHEN** that StudentGroup is deleted
- **THEN** the request is rejected as not found
