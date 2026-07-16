# CLAUDE.md — LostAndFound

School lost-and-found web app. **Not** a "storage box" — it's a *matching exchange + verified-return
workflow*: finders post items, losers subscribe to watch-alerts, the system auto-notifies on a match,
and returns go through a verified two-way handover. Full specs live in **[docs/INDEX.md](docs/INDEX.md)** —
read it before non-trivial work.

> **Status → [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md)** — read it first. It has the FR-by-FR
> state, what to build next, and the project-wide traps. Short version (2026-07-17): FR-AUTH / FR-FOUND /
> FR-TAG / FR-LOG / FR-CLAIM / FR-TL are **done**; notifications are DB-only (**SignalR not built,
> `Hubs/` is empty**); **FR-HOLD, FR-MATCH, FR-CAM, FR-THANK, FR-ADMIN are not started**. Biggest live
> gap: **FR-HOLD** — a Custodial item lands in `PendingDropoff` and is stuck there forever, because no
> staff-intake screen exists to move it to `Open`. **FR-ADMIN is the teammate's — don't build it here.**
> Per-feature lessons + recipes live in [docs/features/](docs/features/); requirements in
> [docs/specs/REQUIREMENTS_2DEV.md](docs/specs/REQUIREMENTS_2DEV.md).

## Tech stack (locked — do not swap versions)
- **.NET 8** (`net8.0`, `LangVersion=12`). SDK on this machine is .NET 10 but the app targets/runs on 8.
- **EF Core 8 — DB-First** (see below) · **SQL Server LocalDB** · **ASP.NET Core Identity** (roles) · **SignalR** *(package referenced; no Hub written yet)* · **Bootstrap 5** · Razor + **TagHelper** + **PartialView**.
- ⚠️ **Bootstrap is pinned at v5.1.0.** `text-bg-*` (5.2+), `bg-*-subtle` (5.3+) and `object-fit-*` (5.3+) **do not exist here and fail SILENTLY** — no build error, no console warning, just an invisible element (a `.badge` with no `bg-*` is white-on-white). **Grep `wwwroot/lib/bootstrap/dist/css/bootstrap.min.css` before using any utility class.** This has bitten us three times.
- Business log = `AuditLog` table (+ optional Serilog). All package refs stay on the **8.0.x** line.

## ⚠️ DB-First (this overrides the Code-First wording in the docs)
The database is the source of truth, **not** the C# classes.
- **`LostAndFound/db/schema.sql`** is the authoritative schema. Change tables THERE first.
- Entities in **`Models/Entities/`** are **generated** by `dotnet ef dbcontext scaffold`. Re-generating overwrites them — use the **`/db-rescaffold`** skill; don't hand-design tables in C#.
- **`Data/ApplicationDbContext.cs`** is the single runtime context (`IdentityDbContext<ApplicationUser>` + the domain DbSets). Hand-written; survives re-scaffold. After a re-scaffold, copy new config from the throwaway `ScaffoldDbContext` into it (see `Data/Scaffolded/README.md`).
- FKs to `AspNetUsers` are plain `string` columns (no navigation) by design.
- **No EF migrations** (that's a Code-First tool). Don't run `migrations add` / `database update`.

## Architecture rules
- **Thin controllers** — they only orchestrate. Business logic goes in a **Service** (`Services/`).
- A service does the rule **+ writes `AuditLog` + pushes `Notification`** in ONE transaction.
- **Never pass an entity to a View** — use a **ViewModel** (`Models/ViewModels/`).
- Forms use **TagHelpers** (`asp-for`, `asp-validation-for`, `asp-action`); reuse via **PartialViews** (`_Name`).
- **Validate in two tiers:** Data Annotations / service checks (code) **and** real DB constraints (already in `schema.sql`). The DB is the last line of defense, not the client.

## Locked enums (`Models/Enums/`) — never rename/reorder
- `FoundItemStatus { PendingDropoff, Open, ClaimAccepted, Returned, Unclaimed, Disposed }`
- `ClaimStatus { Pending, Accepted, Rejected }` · `HoldingType { SelfHeld, Custodial }`
- `CameraRequestStatus { Pending, InReview, Resolved, Rejected }`
- **Holder** = reporter if `SelfHeld`, staff if `Custodial`. Derive it from `HoldingType`; never hardcode by role.

## Shared contracts (`Services/Interfaces/`) — agree before changing
- `ITagService` (Dev A owns impl) · `IAuditService` (Dev A) · `INotificationService` (Dev B). Defined but **not yet DI-registered** — wire them in `Program.cs` when an implementation lands.

## Invariants you must not break
- **One tag normalizer only** (`TagService.Normalize`): trim + lower + strip VN diacritics + collapse spaces. Display uses the raw tag; matching/subscribe ALWAYS compare on `NormalizedTag`.
- **Blind listing:** `PrivateMarks` and claim `VerificationDetails`/evidence are NEVER shown on public pages or the timeline (`AuditLog.IsPublic = 1` rows only).
- State machine: item → `Returned` only when **both** `HolderConfirmedHandover` AND `ClaimantConfirmedHandover`. Accepting one claim auto-rejects the others on that item (one transaction). An item with a `Pending` claim is locked.
- Every status change writes one `AuditLog` row (set `IsPublic` correctly).

## Layout
```
Controllers/        thin; only Home exists in the base
Models/             ApplicationUser.cs (hand) · Enums/ · Entities/ (generated) · ViewModels/
Data/               ApplicationDbContext.cs · SeedData.cs · Scaffolded/ (scaffold target)
Services/           Interfaces/ (contracts) + impls (add as features land)
Hubs/ TagHelpers/   empty until first feature
db/schema.sql       authoritative DB schema
docs/               INDEX.md + the 5 spec docs + features/ (Feature Records)
```

## Commands
```bash
dotnet build LostAndFound/LostAndFound.csproj           # build
dotnet run   --project LostAndFound/LostAndFound.csproj # run (http://localhost:5082)
# recreate DB from the schema:
sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql
# re-scaffold entities (or use the /db-rescaffold skill):
#   see Data/Scaffolded/README.md for the full command
```
Starter admin (seeded): `admin@lostandfound.local` / `Admin#12345` (change before any real deploy).

## Definition of Done (every feature)
2-tier validation · correct `[Authorize]` + ownership check · `AuditLog` written with right `IsPublic` ·
state transitions legal · tags compared on normalized form · no hidden fields leaked · TagHelper +
PartialView UI · `Notification` sent when another user is involved · tested happy path **and** error cases.
**Then** write a Feature Record (`/feature-record`) in `docs/features/`.

## Tooling for this repo
- Skills: **`/feature-slice`** (scaffold a vertical slice), **`/feature-record`** (write the record), **`/db-rescaffold`** (safe DB-First regen).
- Workflow: **`feature-dod-review`** (`/workflows`) — reviews your working changes against the DoD.
- Plugin: the **Superpowers** plugin is recommended (install steps in [docs/INDEX.md](docs/INDEX.md)).

## Git
Branches: `feature/<name>` → PR into `dev` → `main` holds only runnable builds. Commit messages are
concise and reference the `FR-*` id. Link the Feature Record in the PR description.
