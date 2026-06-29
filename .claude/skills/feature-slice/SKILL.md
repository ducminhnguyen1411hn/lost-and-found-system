---
name: feature-slice
description: Scaffold a new vertical slice (ViewModel + Service+Interface + thin Controller + Views/PartialView) for a LostAndFound FR-* feature, following the Feature Playbook anatomy and Definition of Done. Use when starting a new feature so the right files land in the right folders with the required validation/authz/audit/notification wiring.
---

# Scaffold a vertical slice

Build one feature top-to-bottom (Entity → Service → Controller → View). Read
`docs/PROJECT_INSTRUCTION.md` §14 and `docs/FEATURE_PLAYBOOK.md` first.

## Step 1 — classify the feature
Ask (or infer) the **`FR-*` id** and the **dimension** from `docs/FEATURE_PLAYBOOK.md` §2:
- **D1** owner-CRUD · **D2** state-change · **D3** list+search+filter · **D4** multi-step workflow · **D5** realtime/notification.
If a recipe for that dimension already exists in `docs/features/`, follow it ("same as X, differs in…").

## Step 2 — go down the slice anatomy (skip nothing)
1. **Data** — schema change? → edit `db/schema.sql`, then run **`/db-rescaffold`**. (No EF migrations.)
2. **ViewModel** in `Models/ViewModels/` (suffix `...ViewModel`/`...Vm`) with Data Annotations. **Never** bind an entity directly.
3. **Interface + Service** in `Services/Interfaces` + `Services/`. Business rule lives here. Register in `Program.cs` (uncomment/add `AddScoped`).
4. **Controller** — thin: `ModelState.IsValid` → call service → `TempData`/`ModelState`. `[Authorize(Roles=...)]` at the top.
5. **View + PartialView** — Razor + TagHelpers (`asp-for`, `asp-validation-for`, `asp-action`); factor reused chunks into `_Name` partials.

## Step 3 — the cross-cutting must-haves
- **Authorization**: role attribute **and** an ownership/holder check in the service. Holder = derive from `HoldingType` (`SelfHeld`→reporter, `Custodial`→staff), never hardcode by role.
- **State machine**: validate the transition is legal *before* changing `Status`. To `Returned` only when BOTH handover confirmations are true. Accepting a claim auto-rejects siblings — in ONE transaction.
- **AuditLog**: write a row for every important action via `IAuditService`, in the same transaction. Set `IsPublic` correctly (public = safe-for-timeline only).
- **Notification**: if another user is affected, call `INotificationService` (DB + SignalR).
- **Tags**: normalize via the single `ITagService.Normalize`; compare on `NormalizedTag`.
- **No leaks**: never render `PrivateMarks` or claim `VerificationDetails`/evidence on public pages or the timeline.
- **Two-tier validation**: annotations/service checks **and** real DB constraints (already in `schema.sql`).

## Step 4 — test, then record
- Test the happy path **and** error cases (wrong state, missing permission, locked item, boundary data).
- Run `dotnet build` + `dotnet run`; verify in the browser.
- Run the **`feature-dod-review`** workflow (`/workflows`) against your changes.
- Write the Feature Record with **`/feature-record`**.
