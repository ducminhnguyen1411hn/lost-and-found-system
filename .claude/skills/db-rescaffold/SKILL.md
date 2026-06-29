---
name: db-rescaffold
description: Regenerate the DB-First EF Core entities from db/schema.sql after a schema change, safely and without losing hand-written code (ApplicationDbContext, ApplicationUser, enums). Use whenever the database schema changed and Models/Entities needs to be re-synced, or when a new table was added.
---

# Re-scaffold the DB-First entities (safe procedure)

This project is **DB-First**: `LostAndFound/db/schema.sql` is the source of truth and the entity
classes in `Models/Entities/` are generated from the live database. Follow these steps **in order**.

## 0. Before you start
- Confirm the change belongs in **`db/schema.sql`** first. Never add a column/table by editing a
  generated entity — it will be overwritten.
- `dotnet ef` must be the **8.x** tool: `dotnet ef --version` → `8.0.x`. If not:
  `dotnet tool update --global dotnet-ef --version 8.*`.

## 1. Apply the schema change to the database
Edit `db/schema.sql`, then recreate the DB (the script is idempotent; to start clean, drop first):
```
sqlcmd -S "(localdb)\MSSQLLocalDB" -b -Q "IF DB_ID('LostAndFound') IS NOT NULL BEGIN ALTER DATABASE [LostAndFound] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [LostAndFound]; END"
sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql
```

## 2. Re-run the scaffold (regenerates Models/Entities + a throwaway ScaffoldDbContext)
Run from the repo root. `--force` overwrites the generated files; AspNet* Identity tables are
intentionally excluded (Identity owns them via `ApplicationDbContext`):
```
dotnet ef dbcontext scaffold "Server=(localdb)\MSSQLLocalDB;Database=LostAndFound;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --project LostAndFound/LostAndFound.csproj --context ScaffoldDbContext --context-dir Data/Scaffolded --output-dir Models/Entities --no-onconfiguring --use-database-names --no-pluralize --force --table Category --table Location --table LostAlert --table FoundItem --table Tag --table FoundItemTag --table LostAlertTag --table Claim --table CameraCheckRequest --table ThankYou --table Notification --table AuditLog
```
> Add `--table <NewTable>` for any new domain table.

## 3. Merge config into the real context, then delete the throwaway
- Open the regenerated `LostAndFound/Data/Scaffolded/ScaffoldDbContext.cs`.
- Copy any **new** `DbSet<>` lines and new `modelBuilder.Entity<>(...)` blocks into
  `LostAndFound/Data/ApplicationDbContext.cs` (keep `base.OnModelCreating(modelBuilder)` first).
- Delete `Data/Scaffolded/ScaffoldDbContext.cs` again (only `README.md` stays in that folder).

## 4. Verify
```
dotnet build LostAndFound/LostAndFound.csproj
```
Fix references if a renamed column broke feature code. Commit `schema.sql` + `Models/Entities` together.

## Pitfalls
- Don't run `dotnet ef migrations` — this is DB-First, there are no migrations.
- User-FK columns stay plain `string` (no navigation) — that's expected, not a bug.
- If a filtered-index error mentions `QUOTED_IDENTIFIER`, the `SET` lines at the top of
  `schema.sql` handle it; make sure you ran the whole script.
