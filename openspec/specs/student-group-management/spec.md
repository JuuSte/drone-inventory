# student-group-management Specification

## Purpose

Lets staff register the StudentGroups that borrow drones and list them so a StudentGroup can be selected when creating a Loan.

## Requirements

### Requirement: Create StudentGroup
The system SHALL allow creating a StudentGroup with a Name. (REQ-003)

#### Scenario: StudentGroup is created
- **GIVEN** no StudentGroup named "Group 3" exists
- **WHEN** the StudentGroup "Group 3" is created
- **THEN** the StudentGroup is stored

### Requirement: StudentGroup Name is required
The system SHALL reject creating a StudentGroup whose Name is empty or consists only of whitespace.

#### Scenario: Name is required
- **GIVEN** the Name is empty
- **WHEN** a StudentGroup is created
- **THEN** the request is rejected as invalid
- **AND** no StudentGroup is stored

### Requirement: StudentGroup Name is unique
The system SHALL reject creating a StudentGroup whose Name is already used by another StudentGroup that is not deleted. The Name of a deleted StudentGroup SHALL be usable again.

#### Scenario: Duplicate Name
- **GIVEN** a StudentGroup named "Group 3" exists
- **WHEN** another StudentGroup with Name "Group 3" is created
- **THEN** the request is rejected
- **AND** only one StudentGroup named "Group 3" exists

#### Scenario: Name of a deleted StudentGroup is reused
- **GIVEN** the StudentGroup "Group 3" was deleted and has a returned Loan
- **WHEN** a new StudentGroup with Name "Group 3" is created
- **THEN** the new StudentGroup is stored and listed
- **AND** the loan history still shows the returned Loan of the deleted "Group 3"

### Requirement: List StudentGroups
The system SHALL list all StudentGroups with their Name.

#### Scenario: StudentGroups are listed
- **GIVEN** the StudentGroups "Group 1" and "Group 3" exist
- **WHEN** the StudentGroup list is requested
- **THEN** both "Group 1" and "Group 3" are shown

### Requirement: Delete StudentGroup
The system SHALL allow deleting a StudentGroup that has no active Loan, even if it has returned Loans. A deleted StudentGroup SHALL no longer be listed and SHALL NOT be selectable for a Loan, but its Loans SHALL stay unchanged and the loan history SHALL still show its Name (REQ-010). Deleting a StudentGroup with an active Loan SHALL be rejected as a conflict. Deleting a StudentGroup that does not exist or is already deleted SHALL be rejected as not found. (REQ-012)

#### Scenario: StudentGroup with loan history
- **GIVEN** the StudentGroup "Group 3" has only returned Loans
- **WHEN** "Group 3" is deleted
- **THEN** "Group 3" is no longer listed
- **AND** the loan history still shows the Loans of "Group 3" with the StudentGroup Name "Group 3"

#### Scenario: StudentGroup without Loans is deleted
- **GIVEN** the StudentGroup "Group 5" has no Loans
- **WHEN** "Group 5" is deleted
- **THEN** "Group 5" is no longer listed

#### Scenario: Deleted StudentGroup cannot be selected for a Loan
- **GIVEN** the StudentGroup "Group 3" was deleted and the Drone "TELLO-07" is available
- **WHEN** a Loan of "TELLO-07" to "Group 3" is created
- **THEN** the request is rejected as not found
- **AND** "TELLO-07" stays "Available"

#### Scenario: StudentGroup with an active Loan
- **GIVEN** the StudentGroup "Group 3" has an active Loan
- **WHEN** "Group 3" is deleted
- **THEN** the request is rejected as a conflict
- **AND** the active Loan of "Group 3" is still listed

#### Scenario: Unknown StudentGroup
- **GIVEN** no StudentGroup with the requested Id exists
- **WHEN** that StudentGroup is deleted
- **THEN** the request is rejected as not found

#### Scenario: StudentGroup is deleted twice
- **GIVEN** the StudentGroup "Group 5" was deleted
- **WHEN** "Group 5" is deleted again
- **THEN** the request is rejected as not found
