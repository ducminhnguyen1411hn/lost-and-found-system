# Data/Scaffolded — DB-First scaffold target (regenerated, not a runtime context)

This project is **DB-First**. The entity classes in `Models/Entities` are **generated** from the
database by `dotnet ef dbcontext scaffold`. The scaffolder also emits a throwaway context
(`ScaffoldDbContext`) **into this folder** — it is **not** used at runtime and is deleted after its
config is copied into the real `Data/ApplicationDbContext.cs`.

The single runtime context is **`Data/ApplicationDbContext.cs`** (an `IdentityDbContext<ApplicationUser>`).

## When the schema changes
1. Edit **`db/schema.sql`** (the source of truth) — never design tables in C#.
2. Recreate the DB:
   ```
   sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i db\schema.sql
   ```
3. Re-run the scaffold (regenerates `Models/Entities` + a fresh `ScaffoldDbContext` here):
   ```
   dotnet ef dbcontext scaffold "Server=(localdb)\MSSQLLocalDB;Database=LostAndFound;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --context ScaffoldDbContext --context-dir Data/Scaffolded --output-dir Models/Entities --no-onconfiguring --use-database-names --no-pluralize --force --table Category --table Location --table LostAlert --table FoundItem --table Tag --table FoundItemTag --table LostAlertTag --table Claim --table CameraCheckRequest --table ThankYou --table Notification --table AuditLog --table FoundItemImage --table LostItem --table LostItemImage --table LostItemTag
   ```
4. Diff the regenerated `ScaffoldDbContext.cs` against `ApplicationDbContext.cs`; copy any **new**
   `DbSet`s / entity config into `ApplicationDbContext.OnModelCreating` (keep `base.OnModelCreating`
   first). Then **delete `ScaffoldDbContext.cs`** again.

> Follow the four manual steps above exactly (they are the safe path). AspNet* Identity tables are
> intentionally excluded from the `--table` list (Identity owns them via `ApplicationDbContext`).
