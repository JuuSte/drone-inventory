# drone-management Specification

## Purpose

Lets staff register the drones that can be loaned and see every Drone with its current availability, which is derived from whether the Drone has an active Loan.

## Requirements

### Requirement: Create Drone
The system SHALL allow creating a Drone with a Name and a free-text Description. A newly created Drone has no Loan and is therefore "Available". (REQ-001)

#### Scenario: Drone is created
- **GIVEN** no Drone named "TELLO-07" exists
- **WHEN** a Drone with Name "TELLO-07" and Description "SSID: TELLO-9FA21B, right propeller guard damaged" is created
- **THEN** the Drone is stored with that Name and Description
- **AND** its status is "Available"

### Requirement: Drone Name is required
The system SHALL reject creating a Drone whose Name is empty or consists only of whitespace. (REQ-001)

#### Scenario: Name is required
- **GIVEN** the Name is empty
- **WHEN** a Drone is created
- **THEN** the request is rejected as invalid
- **AND** no Drone is stored

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

### Requirement: List Drones
The system SHALL list all Drones with Name, status and Description. The status SHALL be "Loaned" if the Drone has an active Loan and "Available" otherwise; it is derived and never stored. (REQ-002)

#### Scenario: Status derived from active Loan
- **GIVEN** "TELLO-02" has an active Loan and "TELLO-07" has none
- **WHEN** the Drone list is requested
- **THEN** "TELLO-02" is shown as "Loaned"
- **AND** "TELLO-07" is shown as "Available"

#### Scenario: Drone with only returned Loans is available
- **GIVEN** "TELLO-07" has a Loan with Status "Returned" and no active Loan
- **WHEN** the Drone list is requested
- **THEN** "TELLO-07" is shown as "Available"

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
