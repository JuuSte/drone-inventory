# Requirements - Drone Lending System

## Domain Concepts
- **Drone**: a loanable aircraft (Id, Name, Description)
- **StudentGroup**: a group of students that loans drones (Id, Name)
- **Loan**: a loan of one drone to one student group (Id, DroneId, StudentGroupId, LoanDate, ReturnDate, Status: Active | Returned)
- **LoanItem**: an accessory line of a loan (Id, LoanId, Type: Battery | Propeller, Quantity)

## REQ-001 Create Drone
The system SHALL allow creating a drone with a name and a free-text description.
- GIVEN no drone named "TELLO-07" exists
- WHEN a drone with name "TELLO-07" and description "SSID: TELLO-9FA21B, right propeller guard damaged" is created
- THEN the drone is stored and its status is "Available"

Scenario: Name is required
- GIVEN the name is empty
- WHEN a drone is created
- THEN the request is rejected

## REQ-002 List Drones
The system SHALL display all drones with name, status (Available | Loaned) and description.
- GIVEN "TELLO-02" has an active loan and "TELLO-07" has none
- WHEN the drone list is requested
- THEN "TELLO-02" is shown as "Loaned" and "TELLO-07" as "Available"

## REQ-003 Create Student Group
The system SHALL allow creating a student group with a name.
- GIVEN no student group named "Group 3" exists
- WHEN the student group "Group 3" is created
- THEN the student group is stored

## REQ-004 Loan Drone
An available drone SHALL be loaned to a student group.
- GIVEN the drone "TELLO-07" is available
- AND the student group "Group 3" exists
- WHEN "TELLO-07" is loaned to "Group 3"
- THEN an active loan is created with the current date as LoanDate
- AND "TELLO-07" is shown as "Loaned"

## REQ-005 No Double Loan
A drone SHALL have at most one active loan.
- GIVEN "TELLO-07" already has an active loan
- WHEN another group tries to loan "TELLO-07"
- THEN the loan is rejected

## REQ-006 Loan Accessories
A loan SHALL contain quantities of batteries and propellers. Quantities SHALL be integers >= 0.
- GIVEN "TELLO-07" is available and "Group 3" exists
- WHEN "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
- THEN the loan contains a LoanItem Battery with quantity 4 and a LoanItem Propeller with quantity 8

Scenario: Negative quantity
- WHEN a loan is created with -1 batteries
- THEN the loan is rejected

## REQ-007 List Active Loans
The system SHALL display all active loans with group, drone, batteries, propellers and loan date.
- GIVEN "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers on 19.08.2026
- WHEN the active loans are requested
- THEN the row "Group 3 | TELLO-07 | 4 | 8 | 19.08.2026" is shown

## REQ-008 Return Loan
An active loan SHALL be returnable. The drone SHALL become available again.
- GIVEN "TELLO-07" is loaned to "Group 3"
- WHEN the loan is returned
- THEN the loan is marked as "Returned"
- AND "TELLO-07" becomes available again

Scenario: Return twice
- GIVEN the loan is already returned
- WHEN it is returned again
- THEN the request is rejected

## REQ-009 Store Return Date
The system SHALL store the return date when a loan is returned.
- GIVEN an active loan
- WHEN the loan is returned
- THEN ReturnDate is set to the current date

## REQ-010 Loan History
Returned loans SHALL be kept and displayed as loan history.
- GIVEN the loan of "TELLO-07" to "Group 3" was returned
- WHEN the loan history is requested
- THEN the loan is shown with loan date, return date and accessories
- AND the loan is never deleted

## Out of Scope
- Login, user accounts, roles
- Individual battery inventory (BAT-001 ...)
- Reservations in advance
- Damage management, missing accessories on return
- Statistics, reports, exports
