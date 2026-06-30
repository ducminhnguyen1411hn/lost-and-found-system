# Resources — master link list

Every external reference used in this curriculum, grouped by topic. Microsoft Learn links are pinned
to **.NET 8** (`?view=aspnetcore-8.0`). EF Core docs under `/ef/core/` are versionless (apply to EF
Core 8). HTML/CSS use **MDN**, the canonical web reference.

> If a Microsoft Learn link ever opens a newer version, change the `view=` query back to
> `aspnetcore-8.0`.

---

## Start here (the two best end-to-end reads)
- **Get started with ASP.NET Core MVC** (do the whole tutorial once) — https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app/start-mvc?view=aspnetcore-8.0
- **Develop ASP.NET Core MVC apps** (architecture eBook) — https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/develop-asp-net-core-mvc-apps

## C# language (Module 01)
- C# fundamentals — https://learn.microsoft.com/dotnet/csharp/fundamentals/
- Asynchronous programming (async/await) — https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/
- LINQ — https://learn.microsoft.com/dotnet/csharp/linq/
- Nullable reference types — https://learn.microsoft.com/dotnet/csharp/nullable-references
- Interfaces — https://learn.microsoft.com/dotnet/csharp/fundamentals/types/interfaces
- Primary constructors (C# 12) — https://learn.microsoft.com/dotnet/csharp/whats-new/tutorials/primary-constructors

## HTML & CSS (Modules 02–03) — MDN + Bootstrap
- MDN: Learn HTML — https://developer.mozilla.org/en-US/docs/Learn/HTML
- MDN: HTML forms guide — https://developer.mozilla.org/en-US/docs/Learn/Forms
- MDN: HTML element reference — https://developer.mozilla.org/en-US/docs/Web/HTML/Element
- MDN: Learn CSS — https://developer.mozilla.org/en-US/docs/Learn/CSS
- MDN: The box model — https://developer.mozilla.org/en-US/docs/Learn/CSS/Building_blocks/The_box_model
- MDN: Flexbox — https://developer.mozilla.org/en-US/docs/Learn/CSS/CSS_layout/Flexbox
- Bootstrap 5.3 docs — https://getbootstrap.com/docs/5.3/getting-started/introduction/
- Bootstrap grid — https://getbootstrap.com/docs/5.3/layout/grid/
- Bootstrap components — https://getbootstrap.com/docs/5.3/components/
- Bootstrap forms — https://getbootstrap.com/docs/5.3/forms/overview/
- Bootstrap utilities — https://getbootstrap.com/docs/5.3/utilities/spacing/

## ASP.NET Core fundamentals (Module 04)
- Fundamentals overview / Program.cs — https://learn.microsoft.com/aspnet/core/fundamentals/?view=aspnetcore-8.0
- Dependency injection — https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-8.0
- Service lifetimes — https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/service-lifetimes
- Middleware — https://learn.microsoft.com/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0
- Configuration — https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0

## MVC: routing, controllers, views (Modules 05–06, 08)
- MVC overview — https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-8.0
- Routing to controller actions — https://learn.microsoft.com/aspnet/core/mvc/controllers/routing?view=aspnetcore-8.0
- Handle requests with controllers — https://learn.microsoft.com/aspnet/core/mvc/controllers/actions?view=aspnetcore-8.0
- DI into controllers — https://learn.microsoft.com/aspnet/core/mvc/controllers/dependency-injection?view=aspnetcore-8.0
- Views overview — https://learn.microsoft.com/aspnet/core/mvc/views/overview?view=aspnetcore-8.0
- Razor syntax — https://learn.microsoft.com/aspnet/core/mvc/views/razor?view=aspnetcore-8.0
- Layout — https://learn.microsoft.com/aspnet/core/mvc/views/layout?view=aspnetcore-8.0
- Partial views — https://learn.microsoft.com/aspnet/core/mvc/views/partial?view=aspnetcore-8.0

## Models, binding, validation, forms (Modules 07, 09)
- Model binding — https://learn.microsoft.com/aspnet/core/mvc/models/model-binding?view=aspnetcore-8.0
- Model validation — https://learn.microsoft.com/aspnet/core/mvc/models/validation?view=aspnetcore-8.0
- DataAnnotations API — https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations
- Tag Helpers intro — https://learn.microsoft.com/aspnet/core/mvc/views/tag-helpers/intro?view=aspnetcore-8.0
- Tag Helpers in forms — https://learn.microsoft.com/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-8.0
- Built-in Tag Helpers — https://learn.microsoft.com/aspnet/core/mvc/views/tag-helpers/built-in/?view=aspnetcore-8.0
- Anti-forgery / CSRF — https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0

## EF Core 8 — DB-First (Module 10)
- Managing schemas — https://learn.microsoft.com/ef/core/managing-schemas/
- Scaffolding (Reverse Engineering / DB-First) — https://learn.microsoft.com/ef/core/managing-schemas/scaffolding/
- Querying data — https://learn.microsoft.com/ef/core/querying/
- Loading related data (Include) — https://learn.microsoft.com/ef/core/querying/related-data/
- Tracking vs no-tracking — https://learn.microsoft.com/ef/core/querying/tracking
- Saving data — https://learn.microsoft.com/ef/core/saving/
- Transactions — https://learn.microsoft.com/ef/core/saving/transactions
- Async (EF Core) — https://learn.microsoft.com/ef/core/miscellaneous/async
- Tutorial: EF Core in MVC *(Code-First — read for query/CRUD patterns, skip migrations)* — https://learn.microsoft.com/aspnet/core/data/ef-mvc/intro?view=aspnetcore-8.0

## Identity, authentication, authorization (Module 11)
- ASP.NET Core Identity — https://learn.microsoft.com/aspnet/core/security/authentication/identity?view=aspnetcore-8.0
- Simple authorization (`[Authorize]`) — https://learn.microsoft.com/aspnet/core/security/authorization/simple?view=aspnetcore-8.0
- Role-based authorization — https://learn.microsoft.com/aspnet/core/security/authorization/roles?view=aspnetcore-8.0
- Resource-based (ownership) authorization — https://learn.microsoft.com/aspnet/core/security/authorization/resourcebased?view=aspnetcore-8.0

## Architecture & services (Module 12)
- Architectural principles — https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles
- Common web app architectures — https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures
- Working with data in ASP.NET Core apps — https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/work-with-data-in-asp-net-core-apps

## SignalR (Module 13)
- Introduction to SignalR — https://learn.microsoft.com/aspnet/core/signalr/introduction?view=aspnetcore-8.0
- Get started tutorial — https://learn.microsoft.com/aspnet/core/tutorials/signalr?view=aspnetcore-8.0
- Hubs — https://learn.microsoft.com/aspnet/core/signalr/hubs?view=aspnetcore-8.0
- IHubContext (push from a service) — https://learn.microsoft.com/aspnet/core/signalr/hubcontext?view=aspnetcore-8.0
- JavaScript client — https://learn.microsoft.com/aspnet/core/signalr/javascript-client?view=aspnetcore-8.0
- Users and groups — https://learn.microsoft.com/aspnet/core/signalr/groups?view=aspnetcore-8.0

---

## In-repo references (read these too)
- [`../../CLAUDE.md`](../../CLAUDE.md) — the house rules (always loaded by Claude Code).
- [`../INDEX.md`](../INDEX.md) — the master index: roles, data model, state machines, conventions.
- [`../specs/`](../specs/) — the five source spec docs (English translations).
- [`../specs/FEATURE_PLAYBOOK.md`](../specs/FEATURE_PLAYBOOK.md) — the canonical vertical-slice anatomy.
- [`../specs/REQUIREMENTS_2DEV.md`](../specs/REQUIREMENTS_2DEV.md) — the `FR-*` feature list + 2-dev split.
- [`../features/`](../features/) — Feature Records (write one per finished feature via `/feature-record`).

## Tip: search official docs from inside Claude Code
This repo's environment has the **Microsoft Learn** tool wired in. You can ask Claude to look up the
.NET 8 docs for any API instead of guessing — it pulls from the same Microsoft Learn sources linked
above.
