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
The system SHALL reject creating a StudentGroup whose Name is already used by another StudentGroup.

#### Scenario: Duplicate Name
- **GIVEN** a StudentGroup named "Group 3" exists
- **WHEN** another StudentGroup with Name "Group 3" is created
- **THEN** the request is rejected
- **AND** only one StudentGroup named "Group 3" exists

### Requirement: List StudentGroups
The system SHALL list all StudentGroups with their Name.

#### Scenario: StudentGroups are listed
- **GIVEN** the StudentGroups "Group 1" and "Group 3" exist
- **WHEN** the StudentGroup list is requested
- **THEN** both "Group 1" and "Group 3" are shown

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
