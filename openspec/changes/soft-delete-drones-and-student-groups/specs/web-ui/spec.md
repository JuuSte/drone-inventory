# Spec Delta

## MODIFIED Requirements

### Requirement: Delete from the list
Every row of the Drones table and of the StudentGroups table SHALL have a "Delete" button whose accessible name includes the Name of its Drone or StudentGroup. After a successful delete the row SHALL disappear and a short confirmation SHALL be shown. When the backend rejects the delete, its message SHALL be shown in an error alert and the row SHALL stay. The button SHALL be disabled while its request is running and usable with the keyboard only. (UI-008)

#### Scenario: Drone is deleted from the Drones page
- **GIVEN** the Drone "TELLO-08" has no Loans
- **WHEN** the user presses "Delete" in the row of "TELLO-08"
- **THEN** the row of "TELLO-08" is removed from the Drones table
- **AND** a confirmation is shown

#### Scenario: Drone with only returned Loans is deleted from the Drones page
- **GIVEN** the Drone "TELLO-07" has only returned Loans
- **WHEN** the user presses "Delete" in the row of "TELLO-07"
- **THEN** the row of "TELLO-07" is removed from the Drones table
- **AND** the History page still shows the Loans of "TELLO-07"

#### Scenario: Rejected delete keeps the row
- **GIVEN** the Drone "TELLO-07" has an active Loan
- **WHEN** the user presses "Delete" in the row of "TELLO-07"
- **THEN** the backend error message is shown in an error alert
- **AND** the row of "TELLO-07" is still in the Drones table

#### Scenario: StudentGroup is deleted from the Student Groups page
- **GIVEN** the StudentGroup "Group 5" has no Loans
- **WHEN** the user presses "Delete" in the row of "Group 5"
- **THEN** the row of "Group 5" is removed from the StudentGroups table
- **AND** a confirmation is shown

#### Scenario: Rejected StudentGroup delete keeps the row
- **GIVEN** the StudentGroup "Group 3" has an active Loan
- **WHEN** the user presses "Delete" in the row of "Group 3"
- **THEN** the backend error message is shown in an error alert
- **AND** the row of "Group 3" is still in the StudentGroups table

#### Scenario: Delete button names its row
- **WHEN** the Drones page lists "TELLO-07"
- **THEN** the Delete button of that row has the accessible name "Delete Drone TELLO-07"

#### Scenario: Button is disabled while deleting
- **GIVEN** the user pressed "Delete" in the row of "TELLO-08"
- **WHEN** the request has not finished yet
- **THEN** the Delete button of that row is disabled
