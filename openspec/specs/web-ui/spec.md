# web-ui Specification
## Purpose

Defines how the web frontend presents Drones, StudentGroups and Loans to its users: a consistent layout and navigation, readable status and tables, clear feedback on errors and success, and operation by keyboard and at mobile width. It adds no new domain behavior.

## Requirements

### Requirement: Consistent layout and navigation
Every page SHALL show the same top navigation with the links Drones, Student Groups, Loans and History, mark the link of the current page as active, and show a page title. The navigation SHALL stay visible while the page content scrolls. (UI-001)

#### Scenario: Current page is marked in the navigation
- **GIVEN** the user is on the Loans page
- **WHEN** the navigation is shown
- **THEN** the link "Loans" is marked as the current page
- **AND** the links Drones, Student Groups and History are not marked

#### Scenario: Every page has a title
- **WHEN** any of the pages Drones, Student Groups, Loans or History is opened
- **THEN** the page shows a single main heading with the page name

### Requirement: Status shown as badge
Drone status (Available | Loaned) and Loan Status (Active | Returned) SHALL be shown as a labeled badge. Available and Returned SHALL use the success style, Loaned and Active the warning style; errors SHALL never use these styles. The status text SHALL always be shown, so the status is not conveyed by color alone. (UI-002)

#### Scenario: Available and Loaned Drones have different badges
- **GIVEN** the Drone "TELLO-07" is loaned and the Drone "TELLO-08" is not
- **WHEN** the Drones page is shown
- **THEN** "TELLO-07" shows the badge "Loaned" in the warning style
- **AND** "TELLO-08" shows the badge "Available" in the success style

#### Scenario: Loan status badges
- **GIVEN** an active Loan and a returned Loan exist
- **WHEN** the Loans page and the History page are shown
- **THEN** the active Loan shows the badge "Active" and the returned Loan shows the badge "Returned"

### Requirement: Readable tables
Lists of Drones, StudentGroups, active Loans and loan history SHALL be shown as tables with a header row, battery and propeller quantities right-aligned, dates as dd.MM.yyyy, and an empty-state text when there are no rows. (UI-003)

#### Scenario: Active Loan row is readable
- **GIVEN** "TELLO-07" was loaned to "Group 3" on 19.08.2026 with 4 batteries and 8 propellers
- **WHEN** the Loans page is shown
- **THEN** the row shows "Group 3", "TELLO-07", 4, 8 and "19.08.2026"
- **AND** the quantities 4 and 8 are right-aligned

#### Scenario: Empty list
- **GIVEN** no Drones exist
- **WHEN** the Drones page is shown
- **THEN** the table shows the text "No drones yet."

### Requirement: Clear error messages
When the backend rejects a request, the page SHALL show the message from the backend error response in an error alert above the form it belongs to. The alert SHALL be announced to assistive technology and SHALL be removed when the form is submitted again. (UI-004)

#### Scenario: Backend error is shown above the form
- **GIVEN** a Drone named "TELLO-07" exists
- **WHEN** the user creates another Drone named "TELLO-07"
- **THEN** the backend error message is shown in an error alert above the create form
- **AND** the Drones table is unchanged

#### Scenario: Error is cleared on the next submit
- **GIVEN** an error alert is shown on the Drones page
- **WHEN** the user submits the create form again
- **THEN** the previous error alert is removed before the new result is shown

### Requirement: Form feedback
Each form SHALL show a visible label above every input, mark required fields, and have one primary submit button that is disabled while the request is running. After a successful create or return the page SHALL show a short confirmation. (UI-005)

#### Scenario: Button is disabled while saving
- **GIVEN** the user submitted the Drone create form
- **WHEN** the request has not finished yet
- **THEN** the submit button is disabled

#### Scenario: Confirmation after a Loan is returned
- **GIVEN** "TELLO-07" is loaned to "Group 3"
- **WHEN** the user returns the Loan
- **THEN** a confirmation is shown and the Loan is no longer in the active Loans table

#### Scenario: Required fields are marked
- **WHEN** the Drone create form is shown
- **THEN** the Name field is marked as required and the Description field is not

### Requirement: Keyboard accessibility
All actions (navigation, creating a Drone or StudentGroup, creating and returning a Loan) SHALL be usable with the keyboard only. Every input SHALL have an accessible label, every button a text label, the focused element SHALL have a visible focus indicator, and text SHALL have a contrast ratio of at least 4.5:1. (UI-006)

#### Scenario: Loan is created with the keyboard only
- **GIVEN** the Drone "TELLO-07" is available and the StudentGroup "Group 3" exists
- **WHEN** the user moves through the Loan form with Tab, selects "TELLO-07" and "Group 3", enters 4 and 8 and presses Enter
- **THEN** the Loan is created

#### Scenario: Every input has a label
- **WHEN** any page with a form is shown
- **THEN** every input and select has an associated label

#### Scenario: Focus is visible
- **WHEN** a link, input or button receives keyboard focus
- **THEN** a visible focus indicator is shown on it

### Requirement: Usable at mobile width
All pages SHALL be usable at a viewport width of 375px without the page itself scrolling horizontally. The navigation SHALL wrap onto several lines, and wide tables SHALL scroll horizontally inside their own area. (UI-007)

#### Scenario: Loans page at 375px
- **GIVEN** the viewport is 375px wide
- **WHEN** the Loans page is shown with active Loans
- **THEN** the page has no horizontal page scrollbar
- **AND** the active Loans table can be scrolled horizontally within its card
- **AND** the Return button of every row can be reached
