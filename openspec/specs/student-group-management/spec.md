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
