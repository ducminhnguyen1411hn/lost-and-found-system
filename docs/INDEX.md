# LostAndFound — Master Index (file tổng)

Single entry point for the team. Skim this first, then dive into the spec docs or `CLAUDE.md`.

> ℹ️ **Spec docs**: the 5 source documents (PRODUCT_OVERVIEW, PROJECT_INSTRUCTION, REQUIREMENTS_2DEV,
> DIAGRAMS, FEATURE_PLAYBOOK) now live in **[`docs/specs/`](specs/)** as faithful **English translations**
> of the Vietnamese originals. This INDEX distills the essentials; read the full spec in `docs/specs/`
> when you need the detail.

---

## 1. What this is
A school **lost-and-found matching + verified-return** web app. Not a "storage box": finders post
items, people looking for something subscribe to **watch-alerts**, the system **auto-notifies** on a
match, and returns go through a **two-way confirmed handover** with an audit trail. Default trust
model: the finder keeps the item and returns it directly; staff only step in for valuables or disputes.

Three core values (not CRUD): **(1)** publish/subscribe **matching**, **(2)** verified **return
workflow**, **(3)** realtime **notifications**.

## 2. Read order
1. This file → 2. `CLAUDE.md` (conventions, always loaded by Claude Code) → 3. `docs/BASE_SETUP.md`
(how the foundation was built) → 4. the 5 spec docs in [`docs/specs/`](specs/).

> 🚀 **New machine? Get it running first:** follow **[`docs/DEV_SETUP_VSCODE.md`](DEV_SETUP_VSCODE.md)**
> — install · build · run in VSCode, step by step (incl. the mandatory first-run DB create).

## 3. Roles (ASP.NET Core Identity)
| Role | Who | Can |
|---|---|---|
| `Guest` | not logged in | browse public found-item list + detail |
| `Member` | student / lecturer | report found items, watch-alerts, claim, approve/reject claims **on items they hold**, confirm handover, thank, request camera check |
| `Staff` | security / proctor | hold custodial items, process claims for stored items, **arbitrate disputes**, handle camera requests |
| `Admin` | administrator | manage users/roles, Category (2-level)/Location/Tag, dispose unclaimed items, dashboard, audit log |

Claim-approval rights follow **who holds the item** (`HoldingType`), not the role directly.

## 4. Data model (summary)
Entities: `ApplicationUser` (Identity; profile = FullName + Email + PhoneNumber — no school-specific fields), `Category`
(self-ref, 2-level), `Location`, `LostAlert` (watch subscription), `FoundItem`, `Tag`
(DisplayTag + **NormalizedTag UNIQUE**), `FoundItemTag`, `LostAlertTag`, `Claim`,
`CameraCheckRequest`, `ThankYou`, `Notification`, `AuditLog`. Full DDL = **`LostAndFound/db/schema.sql`**.

Three field groups on `FoundItem` — keep them distinct:
- **Structured** (`CategoryId`, `LocationId`, `FoundAt`, tags) → used for **matching**.
- **Free text** (`Title`, `Description`) → used for **search (LIKE)** only, never matching.
- **Hidden** (`PrivateMarks`) → owner-verification; only holder/staff see it. Never public.

## 5. State machines
**FoundItem** (`FoundItemStatus`):
```
create SelfHeld  ─────────────► Open
create Custodial ► PendingDropoff ─(staff intake)─► Open
Open ─(holder accepts a claim)─► ClaimAccepted
ClaimAccepted ─(holder confirms "handed over" AND claimant confirms "received")─► Returned ✓
ClaimAccepted ─(holder cancels)─► Open
Open ─(30/60 days)─► Unclaimed ─► Disposed
```
**Claim** (`ClaimStatus`): `Pending → Accepted` (other claims on the item auto-→ `Rejected`) or
`Pending → Rejected` (+RejectReason).

**Invariants:** at most one `Accepted` claim per item · a `Pending` claim locks the item ·
`Returned` only when BOTH handover confirmations are true · every status change writes one `AuditLog`.

## 6. Locked enums (`Models/Enums/`)
`FoundItemStatus { PendingDropoff, Open, ClaimAccepted, Returned, Unclaimed, Disposed }` ·
`ClaimStatus { Pending, Accepted, Rejected }` · `HoldingType { SelfHeld, Custodial }` ·
`CameraRequestStatus { Pending, InReview, Resolved, Rejected }`. Stored as `int`. **Never rename/reorder.**

## 7. Shared contracts (`Services/Interfaces/`)
`ITagService` (Dev A), `IAuditService` (Dev A), `INotificationService` (Dev B). Defined; not yet
DI-registered (wire in `Program.cs` when an implementation lands). Agree before changing a signature.

## 8. Conventions (full list in `CLAUDE.md`)
Thin controllers · logic in Services (rule + AuditLog + Notification in one transaction) · ViewModels
to views (never entities) · TagHelpers + PartialViews · **one** tag normalizer
(`TagService.Normalize`, compare on `NormalizedTag`) · blind-listing (never leak `PrivateMarks` /
verification) · two-tier validation (code + DB constraints).

## 9. Definition of Done
2-tier validation · correct `[Authorize]` + ownership check · `AuditLog` with right `IsPublic` ·
legal state transitions · tags normalized · no hidden-field leak · TagHelper + PartialView UI ·
`Notification` when another user is involved · happy + error paths tested · **Feature Record written**.

## 10. Folder map
```
LostAndFound/Controllers · Models/{ApplicationUser, Enums, Entities(generated), ViewModels}
            · Data/{ApplicationDbContext, SeedData, Scaffolded} · Services/{Interfaces, impls}
            · Hubs · TagHelpers · Views/Shared · db/schema.sql
docs/ (this index + specs + features/) · .claude/ (skills + workflow + settings)
```

## 11. DB-First workflow (IMPORTANT)
The DB is the source of truth, not the C# classes.
1. Edit `LostAndFound/db/schema.sql`.
2. Recreate DB: `sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql`.
3. Regenerate entities with the **`/db-rescaffold`** skill (wraps `dotnet ef dbcontext scaffold`).
4. Copy new config into `Data/ApplicationDbContext.cs`. **No EF migrations.**

## 12. Getting started
Full step-by-step (fresh machine, VSCode, F5 debug): **[`docs/DEV_SETUP_VSCODE.md`](DEV_SETUP_VSCODE.md)**.
```bash
dotnet build LostAndFound/LostAndFound.csproj
dotnet run   --project LostAndFound/LostAndFound.csproj   # http://localhost:5082 (https 7257)
```
The DB + tables already exist on `(localdb)\MSSQLLocalDB`. Roles + a starter **admin** are seeded:
`admin@lostandfound.local` / `Admin#12345` (change before any real deploy). Login UI is the default
Identity UI at `/Identity/Account/Login` and `/Register`.

## 13. Tooling in this repo
- **Skills** (type `/<name>`): `/feature-slice` (scaffold a vertical slice), `/feature-record`
  (write the record), `/db-rescaffold` (safe DB-First regen).
- **Workflow**: `feature-dod-review` — run via `/workflows`; reviews your working changes against the
  Definition of Done (state machine, authz, audit, notifications, validation, leaks, tags, DB-First).
- **Superpowers plugin** (recommended, one-time): in Claude Code run
  `/plugin marketplace add obra/superpowers-marketplace` then
  `/plugin install superpowers@superpowers-marketplace`
  (or `/plugin install superpowers@claude-plugins-official`).

## 14. Git workflow
`feature/<name>` → PR into `dev` → `main` holds only runnable builds. Concise commit messages
referencing the `FR-*` id. Link the Feature Record in the PR description.

## 15. Work split (2 devs)
- **Dev A** — Found items · Tag · Matching · Holding: `FR-FOUND-*`, `FR-TAG-*`, `FR-MATCH-*`,
  `FR-HOLD-*`, `FR-LOG-*`. Owns `TagService.Normalize`, `MatchingService`, `AuditService`.
- **Dev B** — Accounts · Claim · Notification · Timeline · side modules · Admin: `FR-AUTH-*`,
  `FR-CLAIM-*`, `FR-NOTI-*`, `FR-TL-*`, `FR-CAM-*`, `FR-THANK-*`, `FR-ADMIN-*`. Owns SignalR Hub +
  `NotificationService`.
- Week 1 = shared foundation (this base). Then split by the table above; lock shared contracts (§7)
  and enums (§6) first so both can work without blocking each other.

## 16. 6-week roadmap (from the spec)
1 Foundation (this) · 2 Core CRUD (report/list/detail/search) · 3 Holding + authz + image upload +
audit/timeline · 4 ⭐ Claim + matching + SignalR · 5 Camera/Thanks/Admin/dashboard/disputes ·
6 Polish + validation + tests + report.
