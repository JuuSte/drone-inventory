# Spec Delta

## ADDED Requirements

### Requirement: Delete Drone
The system SHALL delete a Drone only if it has no Loans, neither active nor returned. Deleting a Drone that has at least one Loan SHALL be rejected as a conflict, and the Drone and all its Loans SHALL remain stored unchanged, so loan history is never lost (REQ-010). Deleting a Drone that does not exist SHALL be rejected as not found. (REQ-011)

#### Scenario: Drone without Loans is deleted
- **GIVEN** the Drone "TELLO-08" has no Loans
- **WHEN** "TELLO-08" is deleted
- **THEN** the Drone is removed
- **AND** "TELLO-08" is no longer listed

#### Scenario: Drone with loan history
- **GIVEN** the Drone "TELLO-07" has a Loan with Status "Returned"
- **WHEN** "TELLO-07" is deleted
- **THEN** the request is rejected as a conflict
- **AND** the Drone "TELLO-07" and its Loans remain stored

#### Scenario: Drone with an active Loan
- **GIVEN** the Drone "TELLO-07" has an active Loan
- **WHEN** "TELLO-07" is deleted
- **THEN** the request is rejected as a conflict
- **AND** "TELLO-07" is still listed as "Loaned"

#### Scenario: Unknown Drone
- **GIVEN** no Drone with the requested Id exists
- **WHEN** that Drone is deleted
- **THEN** the request is rejected as not found
