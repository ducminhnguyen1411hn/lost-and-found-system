# Module 05 — MVC & routing

> **Why this matters here:** routing is how a URL finds your code. Get the mental model and you can
> place any new feature's URLs correctly and debug "why is this 404?" in seconds.

**Time:** ~1h · **Prerequisites:** 00, 04 · **Fast track:** ⭐

---

## 1. Conventional routing (what this app uses)

`Program.cs` has exactly one route rule:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

Read the pattern left to right:
- `{controller=Home}` — first URL segment = controller name (default `Home`).
- `{action=Index}` — second segment = action method name (default `Index`).
- `{id?}` — optional third segment, passed as a parameter named `id`.

So the URL maps purely by **names**:

| URL | Controller | Action | id |
|---|---|---|---|
| `/` | `HomeController` | `Index` | — |
| `/FoundItems` | `FoundItemsController` | `Index` | — |
| `/FoundItems/Details/5` | `FoundItemsController` | `Details` | 5 |
| `/Claims/Create` | `ClaimsController` | `Create` | — |

Notice the naming convention: URL says `FoundItems`, the class is `FoundItemsController` (the
`Controller` suffix is dropped in the URL).

📖 [Routing to controller actions](https://learn.microsoft.com/aspnet/core/mvc/controllers/routing?view=aspnetcore-8.0)

---

## 2. View discovery (how the action finds its `.cshtml`)

When an action calls `return View();`, the framework looks for a view named after the action:

```
1. Views/{ControllerName}/{ActionName}.cshtml   ← e.g. Views/FoundItems/Details.cshtml
2. Views/Shared/{ActionName}.cshtml             ← fallback
```

That's why the folder names under `Views/` mirror controller names, and file names mirror action
names. `return View("SomethingElse")` overrides the name; `return View(model)` passes data.

📖 [Views in ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/views/overview?view=aspnetcore-8.0)

---

## 3. The two-action form pattern (GET shows, POST submits)

The single most common shape in MVC: one action **shows** a form, a same-named action **handles** the
submit. Routing picks between them by HTTP verb.

```csharp
// GET /FoundItems/Create  → show the empty form
public IActionResult Create() => View();

// POST /FoundItems/Create → process the submitted form
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateFoundItemVm vm) { /* ... */ }
```

The `[HttpPost]` attribute is what lets two methods share the name `Create` without ambiguity. You'll
use this pattern for report-item, file-claim, subscribe-alert, accept/reject — basically everything.

---

## 4. Generating URLs (don't hardcode them)

Inside views and controllers, build links from controller/action names so they survive refactors:

```cshtml
<!-- in a view, via Tag Helper (Module 09) -->
<a asp-controller="FoundItems" asp-action="Details" asp-route-id="5">View item</a>
```

```csharp
// in a controller, redirect after a successful POST (Post/Redirect/Get pattern)
return RedirectToAction(nameof(Details), new { id = item.Id });
```

The **Post/Redirect/Get** pattern (redirect after a successful POST) prevents the "resubmit form on
refresh" problem. Use it after every create/update.

---

## 🛠️ Exercise

Pure tracing — no code to run:

1. For each URL, name the controller class, action method, and the view file that renders it:
   `/`, `/FoundItems`, `/FoundItems/Details/12`, `/Claims/Create`.
2. You're building `FR-FOUND` (report a found item). Write the **two** action signatures (GET + POST)
   for `FoundItemsController.Create`, with the right attributes. Which view file backs the GET?
3. Write the `RedirectToAction` line that sends the user to the new item's Details page after a
   successful create.
4. Write the anchor Tag Helper that links to that Details page for item id `42`.

---

## ✅ Self-check

- [ ] I can decode `{controller=Home}/{action=Index}/{id?}` and map a URL to a class + method.
- [ ] I know the `FooController` → `/Foo` naming convention.
- [ ] I can explain view discovery (`Views/Controller/Action.cshtml`).
- [ ] I can write the GET+POST two-action pattern with `[HttpPost]`.
- [ ] I know to use `RedirectToAction` (Post/Redirect/Get) after a successful POST.

---

## 📚 Microsoft Learn (.NET 8)

- [Routing to controller actions](https://learn.microsoft.com/aspnet/core/mvc/controllers/routing?view=aspnetcore-8.0)
- [Views overview](https://learn.microsoft.com/aspnet/core/mvc/views/overview?view=aspnetcore-8.0)
- [Tutorial: controller methods and views](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app/controller-methods-views?view=aspnetcore-8.0)

➡️ Next: [Module 06 — Controllers & actions](../06-controllers-and-actions/)
