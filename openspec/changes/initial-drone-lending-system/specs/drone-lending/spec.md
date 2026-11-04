# Spec Delta

## Purpose

Lets staff loan an available Drone with battery and propeller quantities to a StudentGroup, return it again, and keep a permanent loan history of all returned Loans.

## ADDED Requirements

### Requirement: Loan Drone
The system SHALL allow loaning an available Drone to an existing StudentGroup. The new Loan has Status "Active", the current date as LoanDate and no ReturnDate, and the Drone is then "Loaned". (REQ-004)

#### Scenario: Available Drone is loaned
- **GIVEN** the Drone "TELLO-07" is available
- **AND** the StudentGroup "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3"
- **THEN** an active Loan is created with the current date as LoanDate
- **AND** "TELLO-07" is shown as "Loaned"

#### Scenario: Unknown Drone or StudentGroup
- **GIVEN** no Drone or no StudentGroup with the given Id exists
- **WHEN** a Loan is created for it
- **THEN** the Loan is rejected
- **AND** no Loan is stored

### Requirement: No double Loan
A Drone SHALL have at most one active Loan. Creating a Loan for a Drone that already has an active Loan SHALL be rejected. (REQ-005)

#### Scenario: Drone is already loaned
- **GIVEN** "TELLO-07" already has an active Loan
- **WHEN** another StudentGroup tries to loan "TELLO-07"
- **THEN** the Loan is rejected
- **AND** "TELLO-07" still has exactly one active Loan

### Requirement: Loan accessories
A Loan SHALL contain one LoanItem of Type Battery and one LoanItem of Type Propeller with the requested quantities. Quantities SHALL be integers greater than or equal to 0. (REQ-006)

#### Scenario: Loan with batteries and propellers
- **GIVEN** "TELLO-07" is available and "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
- **THEN** the Loan contains a LoanItem Battery with Quantity 4
- **AND** the Loan contains a LoanItem Propeller with Quantity 8

#### Scenario: Negative quantity
- **GIVEN** "TELLO-07" is available and "Group 3" exists
- **WHEN** a Loan is created with -1 batteries
- **THEN** the Loan is rejected
- **AND** "TELLO-07" stays "Available"

### Requirement: List active Loans
The system SHALL list all Loans with Status "Active", showing StudentGroup Name, Drone Name, battery quantity, propeller quantity and LoanDate. Returned Loans SHALL NOT appear in this list. (REQ-007)

#### Scenario: Active Loan is listed
- **GIVEN** "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers on 19.08.2026
- **WHEN** the active Loans are requested
- **THEN** the row "Group 3 | TELLO-07 | 4 | 8 | 19.08.2026" is shown

#### Scenario: Returned Loan is not listed as active
- **GIVEN** the Loan of "TELLO-07" to "Group 3" was returned
- **WHEN** the active Loans are requested
- **THEN** that Loan is not shown

### Requirement: Return Loan
The system SHALL allow returning an active Loan. The Loan's Status becomes "Returned" and the Drone becomes "Available" again. Returning a Loan that is already returned SHALL be rejected. (REQ-008)

#### Scenario: Active Loan is returned
- **GIVEN** "TELLO-07" is loaned to "Group 3"
- **WHEN** the Loan is returned
- **THEN** the Loan is marked as "Returned"
- **AND** "TELLO-07" becomes "Available" again

#### Scenario: Return twice
- **GIVEN** the Loan is already returned
- **WHEN** it is returned again
- **THEN** the request is rejected
- **AND** the Loan's ReturnDate is unchanged

#### Scenario: Unknown Loan
- **GIVEN** no Loan with the given Id exists
- **WHEN** it is returned
- **THEN** the request is rejected as not found

### Requirement: Store return date
The system SHALL set the ReturnDate of a Loan to the current date when the Loan is returned. (REQ-009)

#### Scenario: ReturnDate is set
- **GIVEN** an active Loan
- **WHEN** the Loan is returned
- **THEN** ReturnDate is set to the current date

### Requirement: Loan history
The system SHALL keep every returned Loan permanently and list all Loans with Status "Returned" as loan history, showing StudentGroup Name, Drone Name, LoanDate, ReturnDate and the battery and propeller quantities. Loans SHALL NOT be deletable. (REQ-010)

#### Scenario: Returned Loan appears in history
- **GIVEN** the Loan of "TELLO-07" to "Group 3" was returned
- **WHEN** the loan history is requested
- **THEN** the Loan is shown with LoanDate, ReturnDate and accessories

#### Scenario: Loan is never deleted
- **GIVEN** the Loan of "TELLO-07" to "Group 3" was returned
- **WHEN** "TELLO-07" is loaned and returned again
- **THEN** the loan history contains both Loans of "TELLO-07"
