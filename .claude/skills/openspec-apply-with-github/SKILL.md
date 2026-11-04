---
name: openspec-apply-with-github
description: Implement exactly one GitHub issue of the current OpenSpec change and update the issue status in the GitHub Project.
---

# OpenSpec Apply with GitHub
Always use the GitHub Project "Drone Inventory Project" of owner JuuSte.

## Workflow
1. Identify the GitHub issue to implement (given by the user). Only implement this one task.
2. Set the issue status to "In Progress" in the project
   (`gh project item-list`, `gh project field-list`, `gh project item-edit`)
   and add a comment with the OpenSpec change reference.
3. Make sure you are on a branch `feature/<issue-nr>-<short-name>`.
4. Implement the task according to the OpenSpec specs using the openspec-apply-change skill, including tests for the related scenarios.
5. Run `dotnet test` (backend) and/or `ng test --watch=false` (frontend).
6. Tick the task in tasks.md.
7. Set the status to "Review". Never set "Done" and never archive the change.
