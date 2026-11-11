# Tasks

This change is frontend-only (backend and API unchanged, see design D6). The Given/When/Then scenarios of `web-ui` are covered by Vitest component tests instead of xUnit tests; scenarios that need a real browser (keyboard flow, focus, contrast, 375px) are verified manually.

## 1. Frontend: foundation

- [x] 1.1 Add the design tokens and global base styles (body, headings, form fields, buttons, tables, `.card`, `.alert`, `.table-wrap`, `.num`, `:focus-visible`) to `src/styles.css` (design D1); verify `ng build` succeeds and `ng test --watch=false` still passes (#48)
- [ ] 1.2 Rework the layout shell in `app.html`/`app.css`: sticky wrapping navigation, active link with `aria-current="page"`, centered content max 1100px (design D4), with a Vitest test for scenario "Current page is marked in the navigation"; verify the test passes (#49)
- [ ] 1.3 Add the presentational `app-status-badge` component mapping Available/Returned to success and Loaned/Active to warning (design D2), with Vitest tests for all four statuses; verify the tests pass (#50)

## 2. Frontend: pages

- [ ] 2.1 Restyle the `/drones` page: cards, alert above the form, required Name marker, `novalidate`, status badge, empty state, remove duplicated inline CSS (design D3, D5), with Vitest tests for scenarios "Available and Loaned Drones have different badges", "Backend error is shown above the form", "Error is cleared on the next submit", "Button is disabled while saving", "Required fields are marked" and "Empty list"; verify the tests pass (#51)
- [ ] 2.2 Restyle the `/student-groups` page the same way, with Vitest tests that the alert is above the form and every input has a label; verify the tests pass (#52)
- [ ] 2.3 Restyle the `/loans` page: form card with labels "Drone" and "StudentGroup", alert above the form, active Loans table in `.table-wrap` with right-aligned quantities, Status badge column and "Return Loan" button, confirmation after return (design D3, D5), with Vitest tests for scenarios "Active Loan row is readable", "Loan status badges" (Active), "Confirmation after a Loan is returned" and "Every input has a label"; update existing row expectations for the new column; verify the tests pass (#53)
- [ ] 2.4 Restyle the `/history` page: table card in `.table-wrap`, right-aligned quantities, Status badge column, empty state, with Vitest tests for scenarios "Loan status badges" (Returned) and "Every page has a title"; verify the tests pass (#54)

## 3. Integration

- [ ] 3.1 With API and UI running, check every page at 1280px and 375px in the browser: no horizontal page scroll, wrapping navigation, scrollable tables, visible focus, contrast ≥ 4.5:1, and scenario "Loan is created with the keyboard only" (Tab/Enter only); verify `ng build` and `ng test --watch=false` pass and note the results in the PR (#55)

## Workflow follow-up

- Create GitHub issues from these tasks with `/openspec-to-github`.
- Implement one issue per branch/PR with `/openspec-apply-with-github`.
- Archive with `/openspec-archive-with-github` after all issues are merged.
