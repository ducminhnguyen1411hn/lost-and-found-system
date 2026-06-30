# Module 01 — C# essentials (the subset this project uses)

> **Why this matters here:** you don't need all of C#. You need the ~8 features that appear in every
> controller, service, and entity in this repo. This module is exactly those.

**Time:** ~1.5h · **Prerequisites:** 00 · **Fast track:** ⭐

This is `LangVersion=12` on `net8.0`. The features below are the ones you'll actually read and write.

---

## 1. Classes & properties (auto-properties)

Entities and ViewModels are just classes with **properties** (`get; set;`). Look at the real entity:

```csharp
public partial class FoundItem
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;   // see "nullability" below
    public string? Description { get; set; }      // the ? means "can be null"
    public DateTime FoundAt { get; set; }
    public int Status { get; set; }               // stored as int (DB-First; Module 10 explains)
}
```

- `public int Id { get; set; }` is an **auto-property**: a field + getter + setter in one line.
- `partial` means "this class may be defined across multiple files" — scaffolding uses it so you can
  add code in a separate file without it being overwritten (Module 10).

📖 [Classes (C# guide)](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/classes)
· [Properties](https://learn.microsoft.com/dotnet/csharp/properties)

---

## 2. Nullable reference types (the `?` and the `null!`)

This project compiles with nullable reference types **on**. That changes what `string` means:

- `string Title` → "this should never be null." The compiler warns you if it might be.
- `string? Description` → "this is allowed to be null." You must null-check before using it.
- `= null!;` → "trust me, this gets filled in (by EF/model binding) before anyone reads it." It
  silences the warning for properties that are set by the framework, not the constructor.

This matters for **validation** later: a non-nullable `string` property is treated as **required**
by model binding. Make a field optional by making it `string?`.

📖 [Nullable reference types](https://learn.microsoft.com/dotnet/csharp/nullable-references)

---

## 3. Interfaces (the shared contracts)

An **interface** is a contract: method signatures with no body. This repo uses them so two devs can
agree on a shape before either writes the implementation. Real example:

```csharp
public interface ITagService
{
    string Normalize(string raw);
    Task<IEnumerable<Tag>> ResolveTagsAsync(IEnumerable<string> rawTags);
}
```

A class then *implements* it: `public class TagService : ITagService { ... }`. Controllers depend on
the **interface** (`ITagService`), not the concrete class — that's what makes DI (Module 04) and
testing possible. See [`Services/Interfaces/`](../../../LostAndFound/Services/Interfaces/).

📖 [Interfaces](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/interfaces)

---

## 4. async / await / Task (you'll use this constantly)

Anything that hits the database or network is **async**. The pattern is always the same:

```csharp
public async Task<IActionResult> Index()
{
    // await unwraps the Task and frees the thread while the DB works
    var items = await _context.FoundItem.ToListAsync();
    return View(items);
}
```

Rules to live by:
- A method that awaits must be marked `async` and return `Task` or `Task<T>`.
- `Task<T>` = "a future value of type T." `await` gives you the `T`.
- Use the **`...Async`** version of EF methods (`ToListAsync`, `FirstOrDefaultAsync`,
  `SaveChangesAsync`) and **always `await`** them.
- An EF `DbContext` is **not thread-safe** — never run two queries on it in parallel; `await` one,
  then the next.

Why bother? A web server has limited threads. While one request waits on the DB, `await` lets that
thread serve other users. More throughput, same hardware.

📖 [Asynchronous programming with async/await](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/)
· [Async EF Core](https://learn.microsoft.com/ef/core/miscellaneous/async)

---

## 5. LINQ (querying collections and the database)

LINQ is how you filter, shape, and project data — both in-memory lists and EF queries. The same
syntax works on both:

```csharp
// "Give me Open items in this category, newest first, as a list."
var open = await _context.FoundItem
    .Where(i => i.Status == (int)FoundItemStatus.Open && i.CategoryId == categoryId)
    .OrderByDescending(i => i.CreatedAt)
    .ToListAsync();

// Project entities → a ViewModel (NEVER hand an entity to a view):
var vms = await _context.FoundItem
    .Where(i => i.Status == (int)FoundItemStatus.Open)
    .Select(i => new FoundItemListItemVm { Id = i.Id, Title = i.Title })
    .ToListAsync();
```

Key operators you'll reuse: `Where` (filter), `Select` (transform/project), `OrderBy` /
`OrderByDescending`, `Any` (does anything match?), `Count`, `FirstOrDefault` (first match or null).

**Deferred execution gotcha:** `Where`/`Select`/`OrderBy` only *build* the query — nothing runs until
`ToListAsync()` / `FirstOrDefaultAsync()` forces it. So those terminal operators have async versions;
the builder operators don't.

📖 [LINQ overview](https://learn.microsoft.com/dotnet/csharp/linq/)
· [Querying data (EF Core)](https://learn.microsoft.com/ef/core/querying/)

---

## 6. Collections & casting enums

- `List<T>` and `IEnumerable<T>` are your everyday collection types. `ICollection<Claim> Claim` on
  `FoundItem` is the "many" side of a relationship.
- This repo stores enums as **`int`** columns (DB-First). So you cast both ways:
  `i.Status == (int)FoundItemStatus.Open` and `(FoundItemStatus)i.Status` to read it back.
  The enums are **locked** — never rename or reorder them (the int values are persisted in the DB).

```csharp
// Reading an int column back as the enum for a switch:
var status = (FoundItemStatus)item.Status;
if (status == FoundItemStatus.Open) { /* ... */ }
```

📖 [Enumeration types](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/enum)

---

## 7. Constructor injection & primary constructors

Classes receive their dependencies through the constructor. The classic form (see `HomeController`):

```csharp
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    public HomeController(ILogger<HomeController> logger) => _logger = logger;
}
```

C# 12 (.NET 8) adds **primary constructors** — the same thing, shorter:

```csharp
public class FoundItemsController(ApplicationDbContext db, IAuditService audit) : Controller
{
    // use `db` and `audit` directly in any action
}
```

You'll see both styles. The framework supplies these arguments automatically (Module 04).

📖 [Primary constructors](https://learn.microsoft.com/dotnet/csharp/whats-new/tutorials/primary-constructors)
· [DI into controllers](https://learn.microsoft.com/aspnet/core/mvc/controllers/dependency-injection?view=aspnetcore-8.0)

---

## 🛠️ Exercise

In a scratch file (or [sharplab.io](https://sharplab.io) / a console app), write and run:

1. A class `TagDto` with `int Id`, `string DisplayTag` (non-null), `string? Note` (nullable).
2. A method `string Normalize(string raw)` that does `raw.Trim().ToLowerInvariant()` and collapses
   double spaces. (This is a baby version of the real `TagService.Normalize` — Module 12.)
3. Given `List<TagDto>`, use LINQ to return only those whose `DisplayTag` contains `"phone"`,
   ordered alphabetically, projected to just their `DisplayTag` strings.
4. Write `async Task<int> CountAsync(List<int> xs)` that returns `xs.Count` after
   `await Task.Delay(10)`. Call it with `await`. Notice the `async`/`Task`/`await` triangle.

---

## ✅ Self-check

- [ ] I can explain the difference between `string` and `string?` and why it affects "required".
- [ ] I can write an `async Task<T>` method and `await` an EF `...Async` call.
- [ ] I know not to run two queries on one `DbContext` at the same time.
- [ ] I can filter + project a collection with `Where` + `Select`, and I know when the query runs.
- [ ] I understand why this repo casts `(int)FoundItemStatus.Open` and why the enums are locked.
- [ ] I can read a constructor (classic or primary) and see what dependencies a class needs.

---

## 📚 Microsoft Learn (.NET 8)

- [C# fundamentals](https://learn.microsoft.com/dotnet/csharp/fundamentals/)
- [Asynchronous programming](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/)
- [LINQ](https://learn.microsoft.com/dotnet/csharp/linq/)
- [Nullable reference types](https://learn.microsoft.com/dotnet/csharp/nullable-references)

➡️ Next: [Module 02 — HTML refresher](../02-html-refresher/)
