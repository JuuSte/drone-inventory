---
name: frontend-design
description: Design rules for the Angular frontend of the Drone Lending System. Use for any UI, layout, styling or usability task in frontend/drone-inventory-ui.
---

# Frontend Design - Drone Lending System

## Scope
- Only change templates, CSS and presentational component code.
- Never change backend code, API calls, models or business rules.
- Do not add UI frameworks (Angular Material, Bootstrap, Tailwind) unless the OpenSpec change says so.

## Design tokens
Define them once in src/styles.css as CSS variables and use only these:
- Colors: --bg, --surface, --border, --text, --text-muted, --primary, --success (Available), --warning (Loaned), --danger (errors)
- Spacing: --space-1 (4px), --space-2 (8px), --space-3 (16px), --space-4 (24px), --space-5 (32px)
- Radius: --radius (8px); one font stack (system-ui); base size 16px

## Layout
- Fixed top navigation: app name left, links Drones, Student Groups, Loans, History; mark the active route.
- Content max-width 1100px, centered, page title (h1) on every page.
- Each page: a card with the create form at the top, a card with the list/table below.

## Components
- Tables: header row, zebra or hover rows, right-aligned numbers, dates as dd.MM.yyyy, empty state text ("No drones yet").
- Status as badge: Available = green, Loaned = orange. Loan status Active/Returned the same way.
- Forms: label above input, required fields marked, one primary button per form, disable the button while saving.
- Errors: show the ProblemDetails message from the backend in a red alert above the form; clear it on the next submit.
- Success: short confirmation or the new row appears highlighted.
- Destructive/irreversible actions (Return) need a clear button label, no browser confirm() dialogs.

## Accessibility
- Every input has a <label>; buttons have text; contrast at least 4.5:1; visible focus outline; works with keyboard only.

## Responsive
- Usable at 375px width: navigation wraps, tables scroll horizontally inside their card.

## Verify
- ng build and ng test --watch=false pass.
- Check every page at 1280px and 375px in the browser.
- Use the domain terms Drone, StudentGroup, Loan, LoanItem in labels consistently.
