# Module 10 — EF Core 8 (DB-First)

> **Why this matters here:** this is the data layer, and this repo does it **DB-First** — the opposite
> of most tutorials. The database is the source of truth; entities are *generated*. Getting this model
> right saves you from a whole class of "why did my entity change get wiped?" mistakes.

**Time:** ~2h · **Prerequisites:** 01, 04 · **Fast track:** ⭐

---

## ⚠️ Read this first: DB-First vs Code-First

Most Microsoft tutorials are **Code-First**: you write C# classes, run `migrations`, EF creates the
tables. **This project is the reverse — DB-First:**

| | Code-First (tutorials) | **DB-First (this repo)** |
|---|---|---|
| Source of truth | C# entity classes | **`db/schema.sql`** |
| To change a table | edit C#, `migrations add`, `database update` | **edit `schema.sql`**, recreate DB, **re-scaffold** |
| Entities are | hand-written | **generated** (overwritten on re-scaffold) |
| Migrations | yes | **never** (don't run `migrations`/`database update`) |

So when a Microsoft Learn page tells you to run a migration — **don't**. Translate it: "change the
schema in `schema.sql` instead." Everything else about EF Core (querying, `DbContext`, LINQ) is
identical between the two approaches; only the schema-change workflow differs.

📖 [Managing schemas](https://learn.microsoft.com/ef/core/managing-schemas/) ·
[Scaffolding (Reverse Engineering)](https://learn.microsoft.com/ef/core/managing-schemas/scaffolding/)

---

## 1. The `DbContext` — your gateway to the DB

[`Data/ApplicationDbContext.cs`](../../../LostAndFound/Data/ApplicationDbContext.cs) is the single
runtime context. It's `IdentityDbContext<ApplicationUser>` (so it has the Identity tables) **plus** a
`DbSet` per domain table:

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<FoundItem> FoundItem { get; set; }
    public DbSet<Claim> Claim { get; set; }
    public DbSet<Tag> Tag { get; set; }
    // ...
}
```

Each `DbSet<T>` is a queryable collection mapped to a table. You get the context via DI (it's
registered Scoped by `AddDbContext` in `Program.cs`).

> This file is **hand-written and survives re-scaffold.** The scaffolder writes a *throwaway*
> `ScaffoldDbContext` into `Data/Scaffolded/`; after a re-scaffold you copy any new config from there
> into `ApplicationDbContext`. See `Data/Scaffolded/README.md`.

---

## 2. Reading data (LINQ queries)

You query a `DbSet` with LINQ; the terminal `...Async` call runs the SQL (Module 01 §5).

```csharp
// list: Open items, newest first
var items = await db.FoundItem
    .Where(i => i.Status == (int)FoundItemStatus.Open)
    .OrderByDescending(i => i.CreatedAt)
    .ToListAsync();

// single: one item by id (null if not found)
var item = await db.FoundItem.FirstOrDefaultAsync(i => i.Id == id);

// existence / count
bool hasPendingClaim = await db.Claim.AnyAsync(c => c.FoundItemId == id && c.Status == (int)ClaimStatus.Pending);
```

### Loading related data with `Include`
By default navigations (`item.Category`, `item.Location`) aren't loaded. Pull them in with `Include`:

```csharp
var item = await db.FoundItem
    .Include(i => i.Category)
    .Include(i => i.Location)
    .Include(i => i.FoundItemTag).ThenInclude(ft => ft.Tag)   // many-to-many through join
    .FirstOrDefaultAsync(i => i.Id == id);
```

### Read-only? Add `AsNoTracking`
For list/detail pages you don't intend to edit, `AsNoTracking()` is faster (EF skips change-tracking):

```csharp
var vms = await db.FoundItem.AsNoTracking()
    .Where(i => i.Status == (int)FoundItemStatus.Open)
    .Select(i => new FoundItemListItemVm { Id = i.Id, Title = i.Title, LocationName = i.Location.Name })
    .ToListAsync();
```

Projecting straight into a ViewModel with `.Select(...)` is the cleanest path — it loads only the
columns you need and never exposes the entity.

📖 [Querying data](https://learn.microsoft.com/ef/core/querying/) ·
[Loading related data](https://learn.microsoft.com/ef/core/querying/related-data/) ·
[Tracking vs no-tracking](https://learn.microsoft.com/ef/core/querying/tracking)

---

## 3. Writing data (add / update / delete)

```csharp
// CREATE
var entity = new FoundItem { Title = vm.Title, /* ... */ Status = (int)FoundItemStatus.Open };
db.FoundItem.Add(entity);
await db.SaveChangesAsync();          // INSERT happens here; entity.Id is populated after

// UPDATE — load, mutate, save
var item = await db.FoundItem.FirstOrDefaultAsync(i => i.Id == id);
item!.Status = (int)FoundItemStatus.ClaimAccepted;
await db.SaveChangesAsync();          // EF detects the change and issues UPDATE
```

**Transactions:** `SaveChangesAsync()` is itself atomic — all tracked changes commit together or not
at all. So if you `Add` an `AuditLog` and a `Notification` and update the item, then call
`SaveChangesAsync()` **once**, it's a single transaction. That's exactly how the "rule + audit +
notify in one transaction" rule (Module 12) is satisfied. For work spanning multiple `SaveChanges`
calls, use an explicit `db.Database.BeginTransactionAsync()`.

📖 [Saving data](https://learn.microsoft.com/ef/core/saving/) ·
[Transactions](https://learn.microsoft.com/ef/core/saving/transactions)

---

## 4. Two DB-First quirks you must know

**(a) Enums are `int` columns, with no navigation back.**
The scaffolder maps the `Status` / `HoldingType` columns to `int` (see `FoundItem.Status` is `int`,
not `FoundItemStatus`). That's why you cast everywhere: `i.Status == (int)FoundItemStatus.Open`. The
enums in `Models/Enums/` are **locked** because their integer values are persisted — renaming/reordering
silently corrupts existing rows.

**(b) FKs to `AspNetUsers` are plain `string` columns — no navigation property, by design.**
`FoundItem.ReporterUserId` is a `string`, not a `User` navigation. To get the user, query
`UserManager`/`AspNetUsers` separately. This is a deliberate choice in this repo (keeps the domain
model decoupled from Identity). Don't "fix" it by adding a navigation.

---

## 5. The re-scaffold workflow (when the schema changes)

You will not run `dotnet ef dbcontext scaffold` by hand — use the **`/db-rescaffold`** skill, which
wraps it safely. The flow:

```
1. Edit  LostAndFound/db/schema.sql            (add column / table / constraint)
2. Recreate the DB:
   sqlcmd -S "(localdb)\MSSQLLocalDB" -b -i LostAndFound/db/schema.sql
3. Run the /db-rescaffold skill                (regenerates Models/Entities + a throwaway context)
4. Copy any new mapping config from the throwaway ScaffoldDbContext into ApplicationDbContext.cs
```

Because re-scaffolding **overwrites** `Models/Entities/`, never hand-edit those files — put custom
code in a separate `partial class` file (the entities are generated `partial`).

📖 [Scaffolding command options](https://learn.microsoft.com/ef/core/managing-schemas/scaffolding/) ·
repo: [`../../../LostAndFound/Data/Scaffolded/`](../../../LostAndFound/Data/Scaffolded/)

---

## 🛠️ Exercise

For `FR-FOUND` data access (write the methods; they can live in a service later):

1. `GetOpenItemsAsync()` — returns `List<FoundItemListItemVm>` of Open items, newest first, with
   `LocationName`, using `AsNoTracking` + `.Select(...)` projection.
2. `GetDetailsAsync(int id)` — loads one item **with** `Include(Category)`, `Include(Location)`, and
   its tags via the `FoundItemTag` → `Tag` join; returns the details VM or null.
3. `CreateAsync(CreateFoundItemVm vm, string reporterUserId)` — builds the entity (Status = Open,
   ReporterUserId = the passed string), `Add` + `SaveChangesAsync`, returns the new id.
4. Open `db/schema.sql`. Confirm: which column is `FoundItem.Status`'s type? Is `ReporterUserId` a
   plain `string` FK to `AspNetUsers`? Find the `Tag.NormalizedTag` `UNIQUE` constraint.

---

## ✅ Self-check

- [ ] I can explain DB-First vs Code-First and that this repo never runs migrations.
- [ ] I can query a `DbSet` with `Where`/`OrderBy`/`FirstOrDefaultAsync` and load relations with `Include`.
- [ ] I know `AsNoTracking` + `.Select` into a ViewModel is the read path.
- [ ] I can create/update via `Add`/mutate + a single `SaveChangesAsync`, and I know it's one transaction.
- [ ] I understand why enums are `int` (and locked) and why user FKs are plain strings.
- [ ] I know the re-scaffold flow and that `Models/Entities/` must not be hand-edited.

---

## 📚 Microsoft Learn / EF Core docs

- [Querying data](https://learn.microsoft.com/ef/core/querying/) · [Related data](https://learn.microsoft.com/ef/core/querying/related-data/)
- [Saving data](https://learn.microsoft.com/ef/core/saving/) · [Transactions](https://learn.microsoft.com/ef/core/saving/transactions)
- [Scaffolding / reverse engineering](https://learn.microsoft.com/ef/core/managing-schemas/scaffolding/)
- [Tutorial: EF Core in an ASP.NET MVC app](https://learn.microsoft.com/aspnet/core/data/ef-mvc/intro?view=aspnetcore-8.0) *(it's Code-First — read for the query/CRUD patterns, ignore the migration steps)*

➡️ Next: [Module 11 — Identity, auth & ownership](../11-identity-auth/)
