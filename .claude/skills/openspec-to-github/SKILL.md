---
name: openspec-to-github
description: Create and synchronize GitHub Issues from the tasks of an OpenSpec change and add them to the GitHub Project.
---

# OpenSpec to GitHub
Always use the GitHub Project "Drone Inventory Project" of owner JuuSte
(find the number with `gh project list --owner JuuSte`).

## Workflow
1. Determine the current OpenSpec change (or use the name given as argument).
2. Read proposal.md, design.md, specs/**/*.md and tasks.md of that change.
3. Check existing issues with `gh issue list --state all --limit 200`. Never create duplicates.
4. Create one GitHub Issue per task in tasks.md:
   - Title: task text
   - Body: OpenSpec change name, task number, related requirement/scenario
5. Add each issue to the project with `gh project item-add` and set Status to "Backlog".
6. Write the issue number back into tasks.md behind the task, e.g. `- [ ] 1.1 Drone entity (#12)`.
7. Running this skill twice must not create duplicate issues.
