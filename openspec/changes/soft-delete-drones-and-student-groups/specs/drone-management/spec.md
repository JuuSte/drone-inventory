# Spec Delta

## MODIFIED Requirements

### Requirement: Drone Name is unique
The system SHALL reject creating a Drone whose Name is already used by another Drone that is not deleted. The Name of a deleted Drone SHALL be usable again.

#### Scenario: Duplicate Name
- **GIVEN** a Drone named "TELLO-07" exists
- **WHEN** another Drone with Name "TELLO-07" is created
- **THEN** the request is rejected
- **AND** only one Drone named "TELLO-07" exists

#### Scenario: Name of a deleted Drone is reused
- **GIVEN** the Drone "TELLO-07" was deleted and has a returned Loan
- **WHEN** a new Drone with Name "TELLO-07" is created
- **THEN** the new Drone is stored and listed as "Available"
- **AND** the loan history still shows the returned Loan of the deleted "TELLO-07"

### Requirement: Delete Drone
The system SHALL allow deleting a Drone that has no active Loan, even if it has returned Loans. A deleted Drone SHALL no longer be listed and SHALL NOT be loanable, but its Loans SHALL stay unchanged and the loan history SHALL still show its Name (REQ-010). Deleting a Drone with an active Loan SHALL be rejected as a conflict. Deleting a Drone that does not exist or is already deleted SHALL be rejected as not found. (REQ-011)

#### Scenario: Drone with loan history
- **GIVEN** the Drone "TELLO-07" has only returned Loans
- **WHEN** "TELLO-07" is deleted
- **THEN** "TELLO-07" is no longer listed
- **AND** the loan history still shows the Loans of "TELLO-07" with the Drone Name "TELLO-07"

#### Scenario: Drone without Loans is deleted
- **GIVEN** the Drone "TELLO-08" has no Loans
- **WHEN** "TELLO-08" is deleted
- **THEN** "TELLO-08" is no longer listed

#### Scenario: Deleted Drone cannot be loaned
- **GIVEN** the Drone "TELLO-07" was deleted and the StudentGroup "Group 3" exists
- **WHEN** a Loan of "TELLO-07" to "Group 3" is created
- **THEN** the request is rejected as not found
- **AND** no Loan is stored

#### Scenario: Drone with an active Loan
- **GIVEN** the Drone "TELLO-07" has an active Loan
- **WHEN** "TELLO-07" is deleted
- **THEN** the request is rejected as a conflict
- **AND** "TELLO-07" is still listed as "Loaned"

#### Scenario: Unknown Drone
- **GIVEN** no Drone with the requested Id exists
- **WHEN** that Drone is deleted
- **THEN** the request is rejected as not found

#### Scenario: Drone is deleted twice
- **GIVEN** the Drone "TELLO-08" was deleted
- **WHEN** "TELLO-08" is deleted again
- **THEN** the request is rejected as not found
