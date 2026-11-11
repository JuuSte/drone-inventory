# Proposal

## Why

The Angular frontend works, but each page styles itself with its own hard-coded colors, so the pages look slightly different, Drone status is shown as colored text (Loaned in red, like an error), errors appear below the form, there is no visible focus outline, and tables overflow on a phone. Teachers and students use the system at the drone cabinet, often on a phone, so the UI has to be consistent, readable and usable with keyboard and at mobile width.

## What Changes

- Shared design tokens (colors, spacing, radius, font) in `src/styles.css`; page styles use only these tokens instead of hard-coded values.
- Consistent layout: top navigation with the active route marked, centered content (max 1100px), an `h1` on every page, a card for the form and a card for the list/table.
- Status badges: Drone status Available (green) / Loaned (orange); Loan status Active / Returned shown the same way in the active Loans and History tables.
- Readable tables: header row, hover rows, right-aligned quantities, dates as `dd.MM.yyyy`, empty-state text, horizontal scrolling inside the card on narrow screens.
- Clear error messages: the backend ProblemDetails message is shown in a red alert above the form and cleared on the next submit; a short success confirmation after creating or returning.
- Forms: label above each input, required fields marked, one primary button per form, disabled while saving; the Return button is clearly labeled and visually distinct.
- Keyboard accessibility: every input has a label, visible focus outline, all actions reachable with Tab/Enter, contrast at least 4.5:1.
- Mobile: usable at 375px width; navigation wraps.
- UI labels use the domain terms Drone, StudentGroup, Loan consistently.
- No change to behavior, routes, API calls, models, services or the backend. No UI framework is added.

## Capabilities

### New Capabilities
- `web-ui`: presentation and usability rules of the Angular frontend - layout and navigation, status display, tables, form feedback and error messages, keyboard accessibility and mobile width.

### Modified Capabilities
<!-- none: drone-management, student-group-management and drone-lending behavior is unchanged -->

## Impact

- Frontend only: `frontend/drone-inventory-ui/src/styles.css`, `src/app/app.html` / `app.css`, and the templates/styles of the pages `drones`, `student-groups`, `loans`, `history`; possibly a small presentational status-badge component.
- Existing Vitest page tests are kept; selectors they use (`role="alert"`, `role="status"`, `.loan-list`, `.history-list`, `tbody tr`) stay stable. New Vitest tests cover the `web-ui` scenarios.
- Backend, REST API, models and services: unchanged, so no backend (xUnit) tests are needed for this change.
- No new dependencies.
