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
The system SHALL reject creating a Drone whose Name is already used by another Drone.

#### Scenario: Duplicate Name
- **GIVEN** a Drone named "TELLO-07" exists
- **WHEN** another Drone with Name "TELLO-07" is created
- **THEN** the request is rejected
- **AND** only one Drone named "TELLO-07" exists

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
