# Design

## Context

- Four standalone page components (`drones`, `student-groups`, `loans`, `history`) with inline templates and inline `styles`. Each page copies the same input/button/table/error CSS with hard-coded colors (`#2f6feb`, `#a12a2a`, ...). `src/styles.css` is empty; `app.css` styles the header.
- Drone status is a `<span class="status">` with green/red text; Loan Status is not shown at all on `/loans` or `/history`.
- Error alerts (`role="alert"`) are rendered below the forms; success messages use `role="status"`.
- Existing Vitest page specs rely on `role="alert"`, `role="status"`, `.loan-list`, `.history-list`, `tbody tr`, the `form` element and input `name` attributes. Cell texts are compared exactly.
- The project's frontend-design rules (design tokens, card layout, badges, accessibility, 375px) are the design target. See proposal.md for motivation and `specs/web-ui/spec.md` for the requirements.

## Goals / Non-Goals

**Goals:**
- One source of visual truth (tokens + shared base styles) so all pages look the same.
- Keep the change purely presentational and keep the existing tests green.

**Non-Goals:**
- No changes to services, models, routes, API calls or the backend.
- No UI framework, icon library or web font.
- No dark mode, animations or theming beyond the tokens.

## Decisions

### D1: Tokens and base styles in `src/styles.css`
Define the CSS variables (`--bg`, `--surface`, `--border`, `--text`, `--text-muted`, `--primary`, `--success`, `--warning`, `--danger`, `--space-1..5`, `--radius`, system-ui font, 16px) once in `src/styles.css`, together with global base styles for `body`, `h1`/`h2`, `input`/`select`/`textarea`, `button`, `table`, `.card`, `.alert`, `.form-field` and `:focus-visible`. Page components drop their duplicated inline CSS and keep only page-specific rules, using only the tokens.
- Alternative: a shared component stylesheet imported in every component (`styleUrls`). Rejected - more wiring for the same result; global element styles are fine for an app of four pages.
- Alternative: Angular Material / Tailwind. Rejected - the frontend-design rules forbid adding a UI framework.

### D2: `StatusBadge` presentational component
A small standalone component `app-status-badge` with an input `status: 'Available' | 'Loaned' | 'Active' | 'Returned'` renders `<span class="badge badge--success|warning">{{ status }}</span>`. Mapping: Available, Returned → success; Loaned, Active → warning. It is used in the Drones table, the active Loans table and the History table. The badge text equals the status text, so cell-text assertions in existing tests still match.
- Alternative: a CSS class per page. Rejected - the mapping would be duplicated in three places.

### D3: Page structure
Each page: `<h1>`, then `<section class="card">` with the form (or nothing on History), then `<section class="card">` with an `<h2>` and a `<div class="table-wrap">` (`overflow-x: auto`) around the table. Alerts are moved directly above the form inside the form card. The active Loans and History tables get a Status column (badge). Quantity cells and headers get `class="num"` (right-aligned, `tabular-nums`).

### D4: Layout shell in `app.html` / `app.css`
Sticky header (`position: sticky; top: 0`) with brand left and nav links; `flex-wrap` so it wraps at 375px. `routerLinkActive="active"` plus `ariaCurrentWhenActive="page"` so the active link is also announced. `<main>` is centered with `max-width: 1100px` and side padding `--space-3`.

### D5: Forms and labels
Labels stay wrapped around/above their input (`.form-field`: column layout). Required fields get a visible `*` marker with an `aria-hidden` star and `required` attribute on the input (no client-side blocking beyond what the browser does, validation stays in the backend). Labels use the domain terms: "Drone", "StudentGroup", "Batteries", "Propellers". The Return button gets the text "Return Loan" and a secondary style; no `confirm()` dialog.
- Note: adding `required` lets the browser block an empty submit. To keep backend validation authoritative and the existing "Name is required." test meaningful, the forms use `novalidate`, so the request still goes to the backend and its message is shown.

### D6: Testing
Behavior specs are frontend presentation, so they are verified with Vitest component tests (badge mapping, active nav link/`aria-current`, alert position before the form, alert cleared on submit, disabled button while saving, labels for every input, `num` alignment class, empty-state text). Keyboard flow, focus visibility, contrast and the 375px layout are checked manually in the browser at 1280px and 375px, recorded in the task. No backend tests: the backend is unchanged.

## Risks / Trade-offs

- [Global element styles leak into future components] → keep them few and generic; page-specific rules stay in component styles.
- [Restructured templates break existing selectors] → keep `role` attributes, table classes and input `name`s unchanged; run `ng test --watch=false` after every page task.
- [New Status column changes row cell counts in existing tests] → update the affected expectations in the same task that adds the column.
- [Contrast of orange warning badge] → use dark text on a light orange background (e.g. text `#7a4100` on `#fff1db`) and check ≥ 4.5:1.

## Migration Plan

Pure frontend change, deployed with the next frontend build. Rollback = revert the PRs; no data or API migration.
