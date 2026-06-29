---
name: feature-record
description: Write a Feature Record markdown file in docs/features/ after finishing a LostAndFound feature, following the FEATURE_PLAYBOOK template (lessons learned + reusable recipe). Use as soon as a feature is done, while it's fresh — this is a required step in the Definition of Done.
---

# Write a Feature Record

When a feature is finished, capture it so the next similar screen is faster to build. One file per
feature in **`docs/features/<kebab-name>.md`**. Keep it concrete enough that the other dev (or you in
3 weeks) can rebuild a same-dimension screen without asking. See `docs/FEATURE_PLAYBOOK.md` §3.

## Procedure
1. Pick a filename: `docs/features/<feature>.md` (e.g. `found-item-create.md`, `claim.md`).
2. Fill the template below. Be specific in **A (traps you hit)** and **B (the recipe)** — those are the valuable parts. If the feature reuses an existing dimension recipe, just write "Per recipe `<D?>`, differs in: …".
3. Link the record in the PR description.

## Template (copy, fill, save)
```markdown
# Feature: <name>  (FR-XX-YY)

- **Dimension:** D? (per FEATURE_PLAYBOOK §2)
- **By:** Dev A / Dev B   **Status:** Done / Done + needs refactor
- **Touches:** which Entity / Service / Controller / Views

## A. Lessons & application
- **Business goal:** what pain this solves, for whom.
- **Decisions worth remembering:** what approach, and *why* (short bullets).
- **Traps hit:** the bug/misunderstanding + the fix. (Most valuable section.)
- **Reusable where:** which future screens can copy this.

## B. Reusable recipe
- **Steps (in order):** 1) … 2) … 3) …
- **Files created/changed:** Entity/schema · ViewModel · Service(+Interface) · Controller · View(+Partial).
- **Validation:** code (annotation/service) + DB (constraint).
- **Authorization:** `[Authorize(...)]` where + how the ownership/holder check works.
- **AuditLog:** which actions, `IsPublic`?
- **Notification:** to whom, which type.
- **Minimum tests:** happy path + the error cases that matter.
- **Easy to get wrong when copying:** the gotcha for the next person.
```

## Reminder
This is part of the **Definition of Done** — don't defer records to end of term. After saving, the
feature is only "done" once it also satisfies the DoD checklist in `CLAUDE.md`.
