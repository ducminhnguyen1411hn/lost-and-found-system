# Module 00 — Orientation

> **Why this matters here:** before writing any feature, you need a mental model of what happens
> when someone opens a page, and where each piece of code lives. This module gives you that map.

**Time:** ~30 min · **Prerequisites:** none · **Fast track:** ⭐

---

## 1. What "MVC" means in one picture

MVC = **Model–View–Controller**. It splits a web feature into three jobs so each is easy to change:

- **Model** — the data + the rules. In this app: entities (`FoundItem`, `Claim`…), ViewModels, and
  **Services** that hold business logic.
- **View** — the HTML the user sees. In this app: Razor `.cshtml` files.
- **Controller** — the traffic cop. Takes a request, asks a service/the DB for data, picks a view,
  hands it the data.

The key rule: **the View and Controller depend on the Model, but the Model never depends on them.**
That's why business logic goes in services/models — so it can be tested and reused without a browser.

📖 [Overview of ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-8.0)

---

## 2. The request lifecycle (trace one click)

When a user opens `http://localhost:5082/FoundItems/Details/5`, this happens:

```
Browser
  │  GET /FoundItems/Details/5
  ▼
Kestrel web server
  │
  ▼
Middleware pipeline  (HTTPS redirect → static files → routing → authentication → authorization)
  │
  ▼
Routing matches the pattern "{controller}/{action}/{id?}"
  │   controller = FoundItems, action = Details, id = 5
  ▼
FoundItemsController.Details(5)        ← the ACTION runs
  │   calls a Service / DbContext to load item #5
  │   maps the entity → a ViewModel        (never pass the entity itself!)
  │   return View(viewModel);
  ▼
Razor renders Views/FoundItems/Details.cshtml using the ViewModel
  │   _Layout.cshtml wraps it (nav bar, CSS, footer)
  ▼
HTML response  ──────────────────────────────► Browser paints the page
```

Every feature you build is some variation of this single flow. When something breaks, you debug by
asking "how far down this pipe did the request get?"

📖 [ASP.NET Core fundamentals overview](https://learn.microsoft.com/aspnet/core/fundamentals/?view=aspnetcore-8.0)
· [Routing to controller actions](https://learn.microsoft.com/aspnet/core/mvc/controllers/routing?view=aspnetcore-8.0)

---

## 3. The project map (where things live)

You already have this structure. Memorize the seven folders you'll touch most:

```
LostAndFound/
├─ Program.cs                 ← app startup: services + middleware pipeline (Module 04)
├─ Controllers/              ← thin controllers (only HomeController exists today) (Module 06)
├─ Models/
│  ├─ ApplicationUser.cs      ← hand-written Identity user
│  ├─ Enums/                  ← LOCKED enums (FoundItemStatus, ClaimStatus, …) — never reorder
│  ├─ Entities/               ← GENERATED from the DB by scaffolding — don't hand-edit (Module 10)
│  └─ ViewModels/             ← what you actually pass to views (Module 07)
├─ Data/
│  ├─ ApplicationDbContext.cs ← the runtime EF Core context (hand-written)
│  └─ SeedData.cs             ← seeds roles + the starter admin
├─ Services/
│  ├─ Interfaces/             ← shared contracts (ITagService, IAuditService, INotificationService)
│  └─ (impls land here as features are built) (Module 12)
├─ Views/                     ← Razor pages, grouped by controller (Module 08)
│  └─ Shared/                 ← _Layout.cshtml, partials shared across views
├─ Hubs/                      ← SignalR hubs (empty until Module 13)
├─ TagHelpers/                ← custom tag helpers (empty until needed)
├─ wwwroot/                   ← static files: CSS, JS, images, Bootstrap (Module 03)
└─ db/schema.sql              ← THE source of truth for the database (DB-First)
```

The full rationale for each folder is in [`CLAUDE.md`](../../../CLAUDE.md) and
[`docs/INDEX.md`](../../INDEX.md) — skim both once now.

Open these three real files and just look — you'll understand them fully by the end of the course:
- [`Program.cs`](../../../LostAndFound/Program.cs) — startup
- [`Controllers/HomeController.cs`](../../../LostAndFound/Controllers/HomeController.cs) — a thin controller
- [`Models/Entities/FoundItem.cs`](../../../LostAndFound/Models/Entities/FoundItem.cs) — a generated entity

---

## 4. The dev loop (build, run, see it)

From the repo root:

```bash
# Build
dotnet build LostAndFound/LostAndFound.csproj

# Run (Ctrl+C to stop). Then open the URL it prints.
dotnet run --project LostAndFound/LostAndFound.csproj   # http://localhost:5082  (https 7257)
```

The database + tables already exist on `(localdb)\MSSQLLocalDB`. A starter admin is seeded:
`admin@lostandfound.local` / `Admin#12345`. Login UI lives at `/Identity/Account/Login`.

If you change the database schema, you **don't** run EF migrations (that's a Code-First tool). You
edit `db/schema.sql`, recreate the DB, and re-scaffold the entities — covered in Module 10.

---

## 🛠️ Exercise

1. Run the app and open it in the browser. Log in with the seeded admin account.
2. Open `Controllers/HomeController.cs`. Find the `Index()` action. In the browser, what URL maps to
   it? (Hint: the default route + view discovery.) Confirm by navigating there.
3. Draw the request lifecycle (section 2) **from memory** on paper for the URL `/Home/Privacy`.
   Which file renders the response?
4. In `Models/Entities/FoundItem.cs`, find the `Status` property. What C# type is it? (We'll explain
   *why* it's that type — not an enum — in Module 10. Just notice it now.)

---

## ✅ Self-check

- [ ] I can name the three parts of MVC and say which one holds business logic.
- [ ] I can trace, out loud, how `/FoundItems/Details/5` becomes an HTML page.
- [ ] I know which folder holds generated entities vs. the ones I hand-write (ViewModels, Services).
- [ ] I can build and run the app and log in.
- [ ] I know the database — not the C# classes — is the source of truth in this repo.

---

## 📚 Microsoft Learn (.NET 8)

- [Overview of ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-8.0)
- [ASP.NET Core fundamentals overview](https://learn.microsoft.com/aspnet/core/fundamentals/?view=aspnetcore-8.0)
- [Get started with ASP.NET Core MVC (tutorial series)](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app/start-mvc?view=aspnetcore-8.0) — the single best end-to-end walkthrough; do it once.

➡️ Next: [Module 01 — C# essentials](../01-csharp-essentials/)
