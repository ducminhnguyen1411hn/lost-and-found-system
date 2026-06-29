# Base Setup — how the foundation was built

Record of the foundation work so the team understands what exists and why. **No feature logic** was
built — only the scaffold, data layer, harness, and tooling. Features are assigned per the `FR-*`
items (see `docs/INDEX.md` §15).

## Environment (verified on the setup machine)
- SDK is **.NET 10**, but the project targets **`net8.0`** and the **.NET 8 runtime (8.0.28)** is
  installed → it builds and runs on 8. `LangVersion=12` pins the C# level. All packages are **8.0.x**.
- Database: **SQL Server LocalDB `(localdb)\MSSQLLocalDB`** (created DB `LostAndFound`).
- Installed during setup: **git** (winget) and the **`dotnet-ef` 8.x** global tool.

## Key decisions
- **DB-First** (overrides the Code-First wording in the spec docs): `db/schema.sql` is the source of
  truth; entities are scaffolded into `Models/Entities`. See `CLAUDE.md` → "⚠️ DB-First".
- **Identity integration = Option A**: one `ApplicationDbContext : IdentityDbContext<ApplicationUser>`.
  The scaffold excludes all `AspNet*` tables (Identity owns them); domain user-FKs are plain `string`
  columns (no navigation). This is the simplest robust pattern for a small team.
- **Cascade safety**: every FK is `ON DELETE NO ACTION` (only the tag-join → parent legs cascade).
  This dodges SQL Server's "multiple cascade paths" error (many tables FK to `AspNetUsers` twice) and
  preserves history. User-FK columns are `nvarchar(450)` to match `AspNetUsers.Id`.
- **Seed**: 4 roles + one starter admin (`admin@lostandfound.local` / `Admin#12345`). Fuller seed
  (sample members/categories/locations) is left to FR-AUTH-04.

## What the base contains
- `db/schema.sql` — Identity tables (+ custom columns) and the 12 domain tables, with UNIQUE/CHECK/
  FK/index constraints.
- `Models/Entities/` — generated entities. `Models/ApplicationUser.cs`, `Models/Enums/` (4 locked
  enums) — hand-written.
- `Data/ApplicationDbContext.cs` (runtime context) + `Data/SeedData.cs`. `Data/Scaffolded/` is the
  regenerate-only scaffold target.
- `Services/Interfaces/` — the 3 shared contracts (no impl, not DI-registered yet).
- `Program.cs` — EF + Identity(+roles) + SignalR wired; `UseAuthentication` added; `MapRazorPages`
  for the default Identity UI; commented placeholders for feature-service registration.
- `Views/Shared/_LoginPartial.cshtml` + navbar wiring (required by the Identity UI).
- Empty `Controllers` (Home only), `Hubs/`, `TagHelpers/`, `Models/ViewModels/`, `Services/` impls.

## Verified working
`dotnet build` → 0 warnings / 0 errors. `dotnet run` → home page, `/Identity/Account/Login` and
`/Register` all return **200**; role + admin seeding runs on startup against the LocalDB schema.

## How to extend (per feature)
Use **`/feature-slice`** to scaffold a vertical slice, **`/db-rescaffold`** when the schema changes,
run the **`feature-dod-review`** workflow before finishing, and write a **`/feature-record`**. Follow
the Definition of Done in `CLAUDE.md` / `docs/INDEX.md` §9.
