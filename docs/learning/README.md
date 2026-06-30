# Learning ASP.NET Core MVC — for the LostAndFound project

A focused, **build-the-project-first** curriculum. The goal is *not* to make you an ASP.NET
expert — it is to give you exactly the knowledge you need to ship the LostAndFound features
described in [`../specs/REQUIREMENTS_2DEV.md`](../specs/REQUIREMENTS_2DEV.md), the right way,
following this repo's conventions in [`../../CLAUDE.md`](../../CLAUDE.md).

Everything targets **.NET 8** (the version this app runs on), even though the SDK on the machine
is newer. Primary source = **Microsoft Learn**. For HTML/CSS the primary source is **MDN**
(Mozilla) because that is the canonical, vendor-neutral web reference — Microsoft Learn does not
teach core HTML/CSS.

---

## How to use this folder

Each module is one folder with a `README.md`. Every module follows the same shape so you always
know where to look:

1. **Why this matters here** — one line tying it to LostAndFound.
2. **Core concepts** — short explanation + small C#/Razor code you can read in minutes.
3. **In this project** — where the concept already lives in the real code (clickable file links).
4. **🛠️ Exercise** — a small, real task mapped to an actual `FR-*` feature. Do it; don't just read.
5. **✅ Self-check** — questions to confirm you actually understood it.
6. **📚 Microsoft Learn (.NET 8)** — curated links to go deeper, pinned to `aspnetcore-8.0`.

**Read → type the code yourself → do the exercise → tick the self-check.** Reading alone will not
stick. Type every snippet by hand at least once.

> ℹ️ The exercises are *practice scaffolding*, not graded homework. When you do the **real**
> feature later, use the `/feature-slice` skill — it lays the files out for you the same way these
> exercises teach you to.

---

## The learning path

Do them roughly in order. The web-refresher modules (02–03) can be done any time you feel rusty on
the UI side. If you are short on time, the **Fast track** column shows the minimum to start
contributing.

| # | Module | What you'll be able to do | Time | Fast track |
|---|--------|---------------------------|------|:---:|
| 00 | [Orientation](00-orientation/) | Explain how a request becomes a page; run the app | 30m | ⭐ |
| 01 | [C# essentials](01-csharp-essentials/) | Read/write the C# this codebase uses (async, LINQ, DI) | 1.5h | ⭐ |
| 02 | [HTML refresher](02-html-refresher/) | Hand-write a form + page structure | 1h | ⭐ |
| 03 | [CSS & Bootstrap 5](03-css-and-bootstrap/) | Lay out and style a page with the grid + components | 1.5h | ⭐ |
| 04 | [ASP.NET Core fundamentals](04-aspnetcore-fundamentals/) | Read `Program.cs`; understand DI + middleware | 1.5h | ⭐ |
| 05 | [MVC & routing](05-mvc-and-routing/) | Trace a URL to the action + view that handles it | 1h | ⭐ |
| 06 | [Controllers & actions](06-controllers-and-actions/) | Write a thin controller with GET/POST actions | 1.5h | ⭐ |
| 07 | [Models, ViewModels & validation](07-models-viewmodels-validation/) | Bind a form, validate two-tier, use a ViewModel | 2h | ⭐ |
| 08 | [Views & Razor](08-views-razor/) | Build strongly-typed views, layouts, partials | 2h | ⭐ |
| 09 | [Tag Helpers & forms](09-tag-helpers-and-forms/) | Build forms with `asp-for` + validation + anti-forgery | 1.5h | ⭐ |
| 10 | [EF Core 8 (DB-First)](10-ef-core-db-first/) | Query the DB with LINQ; understand scaffold rules | 2h | ⭐ |
| 11 | [Identity, auth & ownership](11-identity-auth/) | Protect actions with `[Authorize]` + ownership checks | 1.5h | ⭐ |
| 12 | [Services & architecture](12-services-and-architecture/) | Put logic in a service: rule + audit + notify in one tx | 2h | |
| 13 | [SignalR (realtime)](13-signalr-realtime/) | Push a live notification to the browser | 1.5h | |
| 14 | [Capstone: a vertical slice](14-capstone-vertical-slice/) | Build one full `FR-*` feature end-to-end | 3h+ | |

Extra references:
- [`CHECKLISTS.md`](CHECKLISTS.md) — the Definition of Done as a study checklist + a "view rendering" quick-ref.
- [`RESOURCES.md`](RESOURCES.md) — the master list of every Microsoft Learn / MDN link, by topic.

**Minimum to start contributing (the ⭐ fast track):** 00 → 01 → 04 → 05 → 06 → 07 → 08 → 09 → 10 → 11.
That's the loop of *request → controller → service/DB → ViewModel → Razor view → form → auth*, which
is 90% of what every feature touches.

---

## What this project actually is (so the examples make sense)

LostAndFound is **not** a CRUD storage box. It is a **matching exchange + verified-return workflow**:

- Finders **post** found items; people who lost something **subscribe** to watch-alerts by tag.
- The system **auto-notifies** on a tag match.
- Returns go through a **two-way confirmed handover** (both sides confirm) with a full **audit trail**.

So when an exercise says "list found items" or "send a notification on a match", that is a real
slice of the product, not a toy. Read [`../INDEX.md`](../INDEX.md) once for the full picture.

---

## Conventions this curriculum reinforces (from `CLAUDE.md`)

These show up in almost every module on purpose — they are the house rules you must not break:

- **Thin controllers.** Controllers orchestrate; business logic lives in a **Service**.
- **Never pass an entity to a View.** Always use a **ViewModel**.
- **Two-tier validation.** Data Annotations / service checks in code **and** real DB constraints.
- **One tag normalizer** (`TagService.Normalize`); matching always compares on `NormalizedTag`.
- **Blind listing.** `PrivateMarks` and claim verification details are never shown publicly.
- **Every status change writes one `AuditLog` row** (with the correct `IsPublic`).
- **DB-First.** The database (`db/schema.sql`) is the source of truth; entities are *generated*.

---

## Link convention

Microsoft Learn links are pinned to **`?view=aspnetcore-8.0`** so you read the .NET 8 version of the
docs. EF Core docs under `/ef/core/` are versionless (they apply to EF Core 8 too). If a link ever
opens a newer version, just change the `view=` query to `aspnetcore-8.0`.
