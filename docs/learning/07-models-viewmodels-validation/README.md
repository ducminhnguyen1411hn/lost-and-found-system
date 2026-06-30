# Module 07 — Models, ViewModels & validation

> **Why this matters here:** two house rules converge here — **never pass an entity to a view** (use a
> ViewModel) and **two-tier validation** (code + DB). This module is where most feature bugs are
> prevented.

**Time:** ~2h · **Prerequisites:** 01, 06 · **Fast track:** ⭐

---

## 1. Entity vs. ViewModel — and why you must not mix them

- **Entity** (`Models/Entities/`) = a row in a table, *generated* from the DB. It carries everything,
  including secrets like `PrivateMarks` and the full status machine.
- **ViewModel** (`Models/ViewModels/`) = a class shaped for *one screen*. You hand-write it. It
  contains only the fields that screen needs — and **never** the hidden ones.

Why never send the entity to the view?
1. **Security / blind-listing.** `FoundItem.PrivateMarks` is owner-verification data. If you pass the
   entity to a public view, one careless `@Model.PrivateMarks` leaks it. A ViewModel that simply has
   no such property makes the leak *impossible*.
2. **Over-posting.** Binding a form straight onto an entity lets an attacker set fields you didn't
   intend (e.g. `Status`, `ReporterUserId`). A ViewModel only exposes the editable fields.
3. **Shape mismatch.** Views want `CategoryName` (a string); the entity has `CategoryId` (an int) +
   a navigation. The ViewModel does that mapping.

```csharp
// Models/ViewModels/FoundItemDetailsVm.cs — note: NO PrivateMarks, NO raw Status int
public class FoundItemDetailsVm
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string CategoryName { get; set; } = null!;
    public string LocationName { get; set; } = null!;
    public string StatusLabel { get; set; } = null!;   // "Open", from (FoundItemStatus)entity.Status
    public DateTime FoundAt { get; set; }
    public string? ImagePath { get; set; }
}
```

📖 [MVC overview: ViewModels](https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-8.0)

---

## 2. Model binding (form → object)

When a form posts, ASP.NET **model binding** matches each form field's `name` to a property on your
action's parameter and fills it in. This is why HTML `name` attributes matter (Module 02) and why Tag
Helpers (Module 09) generate them from your ViewModel.

```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateFoundItemVm vm) { /* vm is filled from the form */ }
```

A form field `name="Title"` → `vm.Title`. `name="CategoryId"` (a `<select>`) → `vm.CategoryId`.

📖 [Model binding](https://learn.microsoft.com/aspnet/core/mvc/models/model-binding?view=aspnetcore-8.0)

---

## 3. Tier 1: Data Annotations (validation in code)

Put validation attributes on the **input ViewModel**. They drive both server checks and (with the
right scripts) client-side checks.

```csharp
using System.ComponentModel.DataAnnotations;

public class CreateFoundItemVm
{
    [Required, StringLength(120, MinimumLength = 3)]
    public string Title { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }     // optional → nullable

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int LocationId { get; set; }

    [DataType(DataType.Date)]
    public DateTime FoundAt { get; set; }

    // Free-text tags the user typed; the service normalizes them (Module 12).
    public string? TagsCsv { get; set; }
}
```

Common attributes: `[Required]`, `[StringLength]`, `[MaxLength]`, `[Range]`, `[EmailAddress]`,
`[RegularExpression]`, `[Compare]`, `[Display(Name = "...")]`.

**Nullable = optional.** With nullable reference types on, a non-nullable `string` is treated as
`[Required]` by the binder. Make a field genuinely optional by making it `string?`.

In the controller you already saw the guard:
```csharp
if (!ModelState.IsValid) return View(vm);   // ModelState collects all annotation failures
```

📖 [Model validation](https://learn.microsoft.com/aspnet/core/mvc/models/validation?view=aspnetcore-8.0)

---

## 4. Tier 2: the database is the last line of defense

Annotations run in your app — but they can be bypassed (a bug, a direct API call, a race). So the
**real constraints also live in the schema** (`db/schema.sql`): `NOT NULL`, `UNIQUE`, foreign keys,
`CHECK`. Example from this project: `Tag.NormalizedTag` is **`UNIQUE`** in the DB, so even if two
requests race to create the same tag, the database rejects the duplicate.

Your job for every feature: make the code annotations and the DB constraints **agree**. The DB is not
optional belt-and-suspenders — it's the guarantee. Code validation is for nice messages; DB
constraints are for correctness.

> 🔒 This is the **two-tier validation** rule in the Definition of Done. Both tiers, every feature.

📖 [EF Core: keys, indexes & constraints](https://learn.microsoft.com/ef/core/modeling/) (DB-First: you write these in `schema.sql`, not in C#)

---

## 5. Mapping entity ⇄ ViewModel

You'll write small mapping helpers (or do it inline). Keep it explicit and obvious:

```csharp
private static FoundItemDetailsVm ToDetailsVm(FoundItem e) => new()
{
    Id = e.Id,
    Title = e.Title,
    Description = e.Description,
    CategoryName = e.Category.Name,
    LocationName = e.Location.Name,
    StatusLabel = ((FoundItemStatus)e.Status).ToString(),
    FoundAt = e.FoundAt,
    ImagePath = e.ImagePath,
    // PrivateMarks deliberately NOT copied — it must never reach a public view
};
```

For loading the related `Category`/`Location` names you'll use `Include(...)` (Module 10).

---

## 🛠️ Exercise

For `FR-FOUND` (report + view a found item):

1. Write `CreateFoundItemVm` (the input form model) with annotations: `Title` required 3–120 chars,
   `Description` optional ≤1000, `CategoryId`/`LocationId` required, `FoundAt` a date,
   `IsCustodial` bool (SelfHeld vs Custodial), `PrivateMarks` optional ≤500.
2. Write `FoundItemDetailsVm` (the output model) — and deliberately **omit** `PrivateMarks` from the
   *public* version. (For the holder's private view you'd make a separate VM that includes it.)
3. Write the static `ToDetailsVm(FoundItem e)` mapper.
4. In `db/schema.sql`, find the `FoundItem` table. List which of your annotations are *also* enforced
   by a DB constraint (NOT NULL, FK, length). Note any annotation that has **no** DB backstop — that's
   a gap to discuss.

---

## ✅ Self-check

- [ ] I can explain three concrete reasons not to pass an entity to a view.
- [ ] I can write an input ViewModel with Data Annotations and know `string?` = optional.
- [ ] I understand model binding maps form `name`s to ViewModel properties.
- [ ] I can state the two tiers of validation and why the DB is the real guarantee.
- [ ] I can map an entity to a ViewModel while leaving hidden fields out.

---

## 📚 Microsoft Learn (.NET 8)

- [Model binding](https://learn.microsoft.com/aspnet/core/mvc/models/model-binding?view=aspnetcore-8.0)
- [Model validation](https://learn.microsoft.com/aspnet/core/mvc/models/validation?view=aspnetcore-8.0)
- [DataAnnotations API](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations)
- [Part 9: add validation (tutorial)](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app/validation?view=aspnetcore-8.0)

➡️ Next: [Module 08 — Views & Razor](../08-views-razor/)
