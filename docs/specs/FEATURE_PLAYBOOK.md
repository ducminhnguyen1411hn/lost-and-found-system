# Feature Playbook — Conventions for recording & replicating every feature

> **Overall instruction (applies to every feature).** When you finish *any* feature, you must produce a **Feature Record** with two parts: (A) **Lessons & how to apply**, (B) **Reusable recipe**. The goal: learn once, code similar screens faster next time, and let the other person (or yourself three weeks from now) immediately understand why it was done this way.

---

## 0. Why this file exists

This project consists of many **vertical slices** that are structurally similar (reporting a found item, claim, camera request, thank-you note, category management, and so on). Rethinking each screen from scratch every time is wasteful. The convention: **do the first feature of a given type thoroughly → extract a recipe → subsequent features of the same type just clone the recipe and fill in the blanks.**

---

## 1. Rules

1. Every feature (corresponding to one or several `FR-*`) that is completed → create one `docs/features/<feature-name>.md` file following the template in section 3.
2. Write the Feature Record **right after you finish**, while it is still fresh — don't let it pile up to the end of the term.
3. Part (B) "Reusable recipe" must be concrete enough that someone else can **code a same-type screen again without needing to ask**.
4. If the new feature belongs to a **type that already has a recipe** → don't copy the recipe again, just note "follows recipe X, differs in…".
5. When writing the end-of-term report: gathering the Feature Records together is almost the whole "features built + lessons learned" section.

---

## 2. "Feature types" in this project

Most screens fall into one of the following types. Each type should have exactly **one** base recipe, which later screens reuse:

| Type | Examples in the project | Characteristics |
|---|---|---|
| **D1 — CRUD with an owner** | Reporting a found item (FoundItem), LostAlert, Category/Location (admin) | Create/edit/delete, only the owner or admin may edit, 2-tier validation |
| **D2 — Status-change action** | Accept/reject a claim, handover, receiving an item into storage, dispose | Has a state machine, writes AuditLog, usually accompanied by Notification |
| **D3 — Listing + search + filter** | Public list of found items, Staff storage, viewing AuditLog | Pagination, filter, search LIKE, list ViewModel |
| **D4 — Multi-step / multi-party process** | Claim (submit → review → two-way confirmed handover), camera request | Multiple actors, multiple statuses, multiple notifications |
| **D5 — Realtime/Notification** | Notification bell, match alerts, dashboard counter | SignalR group, DB-backed notification |

---

## 3. Feature Record template (copy verbatim, then fill in)

```markdown
# Feature: <Feature name>  (FR-XX-YY)

- **Type:** D? (per the table in section 2)
- **Author:** Dev A / Dev B
- **Status:** Done / Done + needs refactor
- **Touches:** which Entity, which Service, which View

## A. Lessons & how to apply
- **Business goal:** what pain point this feature solves and for whom.
- **Notable decisions:** which approach was chosen, *why* (bullet points, short).
- **Pitfalls encountered:** error/bug/misunderstanding + how it was handled. (This is the most valuable part.)
- **Where it can be reused:** which later screens can apply this experience.

## B. Reusable recipe
> If it's a type that already has a recipe: just note "Follows the <Type> recipe, differs in: …".
> If it's the first feature of a type: fill in the full "Anatomy of a slice" (section 4) for that type.

- **Steps (in order):** 1) … 2) … 3) …
- **Files to create/edit:** Entity / Migration / ViewModel / Service(+Interface) / Controller / View(+Partial).
- **Validation:** what in code (annotation/service) + what in DB (Fluent).
- **Authorization:** where `[Authorize(...)]` goes, how to check "owner".
- **AuditLog:** what action is written, IsPublic?.
- **Notification:** who it's fired to, what type.
- **Minimum tests:** happy path + error case(s) to try.
- **Easy to get wrong when cloning:** notes for whoever builds a similar screen.
```

---

## 4. Anatomy of a vertical slice (the base reference for replication)

This is the common "backbone" of nearly every feature in the project. When building a new screen, go through the layers below in order — if any layer is missing, the feature is not yet "Done" per the Definition of Done.

```
1. Entity            → add/edit fields in /Models/Entities + Fluent API in AppDbContext
2. Migration         → dotnet ef migrations add <Name>; review the migration file before update
3. ViewModel         → /Models/ViewModels, attach Data Annotations (DON'T use the entity as input)
4. Interface + Service → /Services/Interfaces + /Services; business logic + AuditLog + Notification here
5. Controller        → thin: receive VM → ModelState.IsValid → call Service → put the result into TempData/ModelState
6. View + Partial    → Razor + TagHelper (asp-for/asp-validation-for/asp-action); extract the reused part into a _Partial
7. Authorization     → [Authorize(Roles=...)] + ownership check in the Service if needed
8. DB-tier validation → ensure the Fluent API produced a real constraint (NOT NULL/MaxLength/UNIQUE/FK)
9. State + Audit     → if changing status: check validity per the state machine + write AuditLog (set IsPublic)
10. Notification      → if another user is involved: write Notification + push SignalR
11. Test             → happy path + error case (wrong status, missing permission, boundary data)
```

**Quick reference by type:**
- **D1 (CRUD):** focus on steps 1–8. Remember to check "only the owner may edit/delete".
- **D2 (status change):** focus on steps 9–10. Always validate the transition is legal *before* changing.
- **D3 (listing):** focus on step 3 (list ViewModel + pagination) + step 6 (filter form via TagHelper). Little to no involvement with 9–10.
- **D4 (process):** it's a chain of several D2s linked together; draw the state machine first, each step is a D2 action.
- **D5 (realtime):** focus on step 10; clearly define the SignalR group + notification type.

---

## 5. Worked example, filled in — "Submit a claim to get an item back (Claim)"

```markdown
# Feature: Submit & review a claim to get an item back (Claim)  (FR-CLAIM-01..05)

- **Type:** D4 (multi-step process) — composed of D2 actions.
- **Author:** Dev B
- **Status:** Done
- **Touches:** Entity Claim + FoundItem; ClaimService; Views Claim/Create, Claim/Review, _ClaimForm, _Timeline.

## A. Lessons & how to apply
- **Business goal:** let someone who believes they are the owner reclaim an item, with verification to prevent wrong handovers/disputes.
- **Notable decisions:**
  - Lock the FoundItem when there's a Pending claim → avoid reviewing too hastily before enough claimants have gathered.
  - Handover requires 2 confirmations (holder + claimant) before moving to Returned → leaves a trail, no staff needed.
  - "Holder" is derived from HoldingType, not hardcoded by role → the same screen serves both SelfHeld and Custodial.
- **Pitfalls encountered:**
  - When accepting one claim, forgot to automatically Reject the other claims on the same item → 2 claims both Accepted (invariant broken). Fix: combine Accept + Reject-the-other-claims into a single transaction in the Service.
  - Initially RejectReason was nullable but the UI still allowed an empty submit → added validation requiring RejectReason when rejecting.
- **Where it can be reused:** camera request (also a Pending→process→respond flow), any "review one of many" flow.

## B. Reusable recipe
- **Steps:** 1) draw the Claim state machine first. 2) build the Create action (D1-ish). 3) build the Accept/Reject action (D2, with lock + audit + noti). 4) build the two-way confirmed handover (D2 with a 2-flag condition).
- **Files to create/edit:** Entity Claim (+2 confirmation flags on FoundItem) → migration → ClaimCreateVm/ClaimReviewVm → IClaimService/ClaimService → ClaimController → Views + _ClaimForm.
- **Validation:** code: VerificationDetails [Required]; RejectReason required-when-rejecting (service). DB: FK Claim→FoundItem, CHECK ClaimStatus.
- **Authorization:** [Authorize(Roles="Member,Staff")]; in the Service check "is the caller the holder of the item" before allowing Accept/Reject.
- **AuditLog:** action "ClaimCreated"/"ClaimAccepted"/"ClaimRejected"/"Handover"; IsPublic=true for status milestones (to appear on the timeline), false for verification content.
- **Notification:** the holder when there's a new claim; the claimant when accepted/rejected; the other party when their counterpart confirms the handover.
- **Minimum tests:** happy path; claiming when the item is not Open (must be blocked); reviewing while there are multiple claims (the other claims must auto-Reject); moving to Returned when only one party has confirmed (must be blocked).
- **Easy to get wrong when cloning:** always wrap operations that change multiple records in a single transaction; don't forget to set IsPublic correctly, lest verification information leak onto the timeline.
```

---

## 6. Where to store

```
/docs
  /features
    found-item-create.md
    claim.md
    matching-subscribe.md
    camera-request.md
    ...
  feature-playbook.md   (this very file)
```

> Tip: put links to the Feature Records in the PR description. A reviewer who reads the Record immediately understands the intent and reviews faster.
