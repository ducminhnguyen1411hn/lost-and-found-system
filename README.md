# LostAndFound

A school **lost-and-found** web application — not a simple storage log, but a **matching exchange
with a verified-return workflow**: finders post items, searchers subscribe to watch-alerts, the
system auto-notifies on a match, and handovers are confirmed by both parties with a full audit trail.

> **Status: foundation only.** Feature logic is implemented per the `FR-*` items in the specs.
> For domain rules and conventions, read **[CLAUDE.md](CLAUDE.md)** and **[docs/INDEX.md](docs/INDEX.md)**
> before non-trivial work. This file covers **how to build and run the app**.

---

## Tech stack

| Area | Choice |
|---|---|
| Runtime | **.NET 8** (`net8.0`, C# 12) |
| Web | ASP.NET Core MVC — Razor + TagHelpers + Partial Views, Bootstrap 5 |
| Data | **EF Core 8, DB-First** · SQL Server **LocalDB** |
| Auth | ASP.NET Core Identity (roles: `Guest` / `Member` / `Staff` / `Admin`) |
| Realtime | SignalR |
| Audit | `AuditLog` table (+ optional Serilog) |

> Package versions are pinned to the **8.0.x** line. Do not upgrade across major versions.

---

## Prerequisites

| Requirement | Notes |
|---|---|
| **.NET 8 SDK** | The app targets `net8.0`. A .NET 10 SDK can build it as long as the .NET 8 runtime/reference pack is present; if the build reports a missing `net8.0` targeting pack, install the .NET 8 SDK. |
| **SQL Server LocalDB** (`MSSQLLocalDB`) | Ships with Visual Studio or the standalone "SQL Server Express LocalDB" installer. |
| **`sqlcmd`** | Used to build the database from `schema.sql`. Part of the SQL Server command-line utilities. |

Verify your environment:

```powershell
dotnet --list-runtimes | Select-String "AspNetCore.App 8"   # expect 8.0.x
sqllocaldb info                                              # expect MSSQLLocalDB
```

---

## Quick start

Run from the repository root.

```powershell
# 1. Start LocalDB (if not already running)
sqllocaldb start MSSQLLocalDB

# 2. Create the database and tables from the authoritative schema
sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql

# 3. Build
dotnet build LostAndFound/LostAndFound.csproj

# 4. Run
dotnet run --project LostAndFound/LostAndFound.csproj
```

Then open **http://localhost:5082**.

On startup the app seeds the four roles and a starter admin account (idempotent — safe to re-run).

---

## Access

| Item | Value |
|---|---|
| App (HTTP) | http://localhost:5082 |
| App (HTTPS) | https://localhost:7257 |
| Sign in | `/Identity/Account/Login` · Register: `/Identity/Account/Register` |
| **Seeded admin** | `admin@lostandfound.local` / `Admin#12345` |

> The seeded admin password is for local development only — change it before any real deployment.

To run over HTTPS (trust the dev certificate once):

```powershell
dotnet dev-certs https --trust
dotnet run --project LostAndFound/LostAndFound.csproj --launch-profile https
```

---

## Day-to-day development

The database persists between runs, so after the first setup you only need:

```powershell
dotnet run --project LostAndFound/LostAndFound.csproj
```

Stop the app with `Ctrl + C`. For automatic rebuilds on file changes:

```powershell
dotnet watch --project LostAndFound/LostAndFound.csproj
```

---

## Database (DB-First)

The database is the **source of truth**, not the C# entity classes. When the schema changes:

1. Edit [LostAndFound/db/schema.sql](LostAndFound/db/schema.sql).
2. Recreate the database: `sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql`.
3. Regenerate the entities with the **`/db-rescaffold`** skill — do not hand-edit `Models/Entities/`.
4. **Do not** run EF migrations (`migrations add` / `database update`); those are Code-First tools.

See the "DB-First" section of [CLAUDE.md](CLAUDE.md) for the full workflow.

---

## Configuration

The connection string lives in [LostAndFound/appsettings.json](LostAndFound/appsettings.json) under
`ConnectionStrings:DefaultConnection`. The default points at LocalDB:

```
Server=(localdb)\MSSQLLocalDB;Database=LostAndFound;Trusted_Connection=True;TrustServerCertificate=True
```

Application URLs and launch profiles are defined in
[LostAndFound/Properties/launchSettings.json](LostAndFound/Properties/launchSettings.json).

### Secrets — Cloudinary & local overrides

`appsettings.json` is **committed to git**, so it holds only non-secret defaults: the `Cloudinary`
values there are intentionally left **empty**. **Never paste real API keys into `appsettings.json`** —
once committed they stay in git history forever, even if you delete them in a later commit.

Image upload (found/lost item photos) needs a **Cloudinary** account. Put your real keys in **one** of
the places below — both are kept out of git and override `appsettings.json` at runtime:

**Option A — `appsettings.Development.json`** (git-ignored; the simplest "just works" local file):

```json
{
  "Cloudinary": {
    "CloudName": "<your-cloud-name>",
    "ApiKey": "<your-api-key>",
    "ApiSecret": "<your-api-secret>"
  }
}
```

**Option B — user-secrets** (per-developer, stored outside the repo):

```powershell
dotnet user-secrets set "Cloudinary:CloudName" "<your-cloud-name>" --project LostAndFound/LostAndFound.csproj
dotnet user-secrets set "Cloudinary:ApiKey"    "<your-api-key>"    --project LostAndFound/LostAndFound.csproj
dotnet user-secrets set "Cloudinary:ApiSecret" "<your-api-secret>" --project LostAndFound/LostAndFound.csproj
```

You can override `ConnectionStrings:DefaultConnection` the same way (e.g. to point at a real SQL Server
instance instead of LocalDB).

> `appsettings.Development.json` is listed in `.gitignore`. If `git status` still shows it as tracked,
> untrack it once (the file stays on disk): `git rm --cached LostAndFound/appsettings.Development.json`.
> Without Cloudinary configured the app still runs — only image upload fails.

---

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `Cannot open database "LostAndFound"` or a network-related connection error | The schema step hasn't run, or LocalDB is stopped. Run `sqllocaldb start MSSQLLocalDB`, then re-run the `sqlcmd ... schema.sql` step. |
| Build reports a missing `net8.0` targeting pack | Only a newer SDK is installed without the .NET 8 reference pack. Install the **.NET 8 SDK**. |
| `Address already in use` (port 5082 / 7257) | Another instance holds the port. Close it, or change the port in `launchSettings.json`. |
| HTTPS certificate warning at sign-in | Run `dotnet dev-certs https --trust` once. |
| `sqllocaldb` / `sqlcmd` not found | Install "SQL Server Express LocalDB" and the "SQL Server command-line utilities". |

---

## Project structure

```
LostAndFound/
  Controllers/        thin — orchestration only
  Models/
    ApplicationUser.cs (hand-written)
    Enums/             locked domain enums
    Entities/          generated (DB-First scaffold)
    ViewModels/        what views receive (never entities)
  Data/
    ApplicationDbContext.cs   single runtime context
    SeedData.cs               roles + starter admin
    Scaffolded/               scaffold target
  Services/            Interfaces/ (contracts) + implementations
  Hubs/  TagHelpers/    SignalR + UI helpers
  db/schema.sql         authoritative database schema
docs/                   INDEX.md + specs + feature records
```

---

## Documentation

- **[docs/INDEX.md](docs/INDEX.md)** — master index: domain model, state machines, roles, roadmap.
- **[CLAUDE.md](CLAUDE.md)** — architecture rules, invariants, and conventions.
- **[docs/BASE_SETUP.md](docs/BASE_SETUP.md)** — how the foundation was built.

---

## Git workflow

`feature/<name>` → PR into `dev` → `main` holds only runnable builds. Keep commit messages concise
and reference the relevant `FR-*` id; link the Feature Record in the PR description.
