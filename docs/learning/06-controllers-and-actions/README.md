# Module 06 — Controllers & actions

> **Why this matters here:** controllers are where your request handling lives. This repo's #1 rule is
> **thin controllers** — they orchestrate, they don't think. Learn the shape now so you don't end up
> with business logic in the wrong place.

**Time:** ~1.5h · **Prerequisites:** 05 · **Fast track:** ⭐

---

## 1. Anatomy of a controller

A controller is a class ending in `Controller`, inheriting `Controller`, whose public methods are
**actions**. Dependencies arrive via the constructor (Module 04). Real example:

```csharp
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    public HomeController(ILogger<HomeController> logger) => _logger = logger;

    public IActionResult Index() => View();
}
```

A feature controller will inject the services it orchestrates:

```csharp
public class FoundItemsController(
    ApplicationDbContext db,        // read for queries (writes go through a service)
    ITagService tags,
    IAuditService audit) : Controller
{
    // actions...
}
```

📖 [Handle requests with controllers](https://learn.microsoft.com/aspnet/core/mvc/controllers/actions?view=aspnetcore-8.0)

---

## 2. Action return types (`IActionResult`)

Actions return an `IActionResult` — a description of the response. The common ones:

```csharp
return View(vm);                       // render a Razor view with a ViewModel
return RedirectToAction(nameof(Index)); // 302 redirect (after a successful POST)
return NotFound();                      // 404
return Forbid();                        // 403 (logged in but not allowed)
return BadRequest();                    // 400
```

For async actions (anything hitting the DB) the type is `Task<IActionResult>`:

```csharp
public async Task<IActionResult> Details(int id)
{
    var item = await db.FoundItem.FirstOrDefaultAsync(i => i.Id == id);
    if (item is null) return NotFound();          // always handle "not found"
    var vm = MapToDetailsVm(item);                // entity → ViewModel (never return the entity)
    return View(vm);
}
```

📖 [Action return types](https://learn.microsoft.com/aspnet/core/mvc/controllers/actions?view=aspnetcore-8.0)

---

## 3. The canonical GET/POST pair

This is the shape of nearly every write feature. Study it — you'll copy it a dozen times.

```csharp
// GET: show the empty form
public IActionResult Create() => View(new CreateFoundItemVm());

// POST: validate, do the work via a service, redirect
[HttpPost]
[ValidateAntiForgeryToken]                         // CSRF protection — always on POST
public async Task<IActionResult> Create(CreateFoundItemVm vm)
{
    if (!ModelState.IsValid)                        // tier-1 validation failed?
        return View(vm);                            // redisplay form WITH the user's input + errors

    // Thin controller: hand the real work to a service (rule + AuditLog + Notification in one tx)
    var newId = await _foundItemService.CreateAsync(vm, User);

    return RedirectToAction(nameof(Details), new { id = newId });  // Post/Redirect/Get
}
```

What the controller is allowed to do: read input, check `ModelState`, call a service, choose a result.
What it must **not** do: contain the business rules, write to multiple tables, build notifications.
That's the service's job (Module 12).

📖 [Model validation](https://learn.microsoft.com/aspnet/core/mvc/models/validation?view=aspnetcore-8.0)
· [Prevent CSRF](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0)

---

## 4. Passing data to the view

- **Primary data → the ViewModel** you pass to `View(vm)` (the strongly-typed, preferred way).
- **One-off messages → `TempData`** survives exactly one redirect — perfect for "Item reported!"
  after a Post/Redirect/Get:
  ```csharp
  TempData["Success"] = "Item reported.";
  return RedirectToAction(nameof(Index));
  ```
- `ViewData` / `ViewBag` exist but avoid them for anything structured — they're untyped and error-prone.

---

## 5. Who is the current user?

Inside any action you have `User` (a `ClaimsPrincipal`). You'll use it for ownership checks (Module 11):

```csharp
var myId = userManager.GetUserId(User);   // the AspNetUsers Id of the signed-in user
bool isStaff = User.IsInRole("Staff");
```

Ownership matters in this app: a Member can only accept/reject claims **on items they hold**. The
controller (or service) must verify `item.ReporterUserId == myId` (or staff/custodian) before acting.

---

## 🛠️ Exercise

Sketch (you don't need it to compile yet) a `FoundItemsController` for `FR-FOUND`:

1. Constructor injecting `ApplicationDbContext` and a (future) `IFoundItemService`.
2. `Index()` — `async Task<IActionResult>` that loads **Open** items and returns a list ViewModel.
   (Just write the LINQ + `return View(vms)`; the ViewModel comes in Module 07.)
3. `Details(int id)` — loads one item, returns `NotFound()` if missing, else a details ViewModel.
4. `Create()` GET + `Create(CreateFoundItemVm vm)` POST with `[HttpPost]` +
   `[ValidateAntiForgeryToken]`, the `ModelState.IsValid` guard, a service call, and a
   `RedirectToAction` to `Details`. Add a `TempData["Success"]` message.

Keep every method under ~10 lines. If one grows, that logic belongs in a service.

---

## ✅ Self-check

- [ ] I can write a controller with constructor-injected dependencies.
- [ ] I know what `IActionResult` is and can return `View`, `RedirectToAction`, `NotFound`.
- [ ] I can write the GET/POST pair with `[HttpPost]` + `[ValidateAntiForgeryToken]`.
- [ ] I check `ModelState.IsValid` and redisplay the form (with input) on failure.
- [ ] I can articulate what a thin controller may do vs. what belongs in a service.
- [ ] I know how to get the current user's id and check a role.

---

## 📚 Microsoft Learn (.NET 8)

- [Handle requests with controllers](https://learn.microsoft.com/aspnet/core/mvc/controllers/actions?view=aspnetcore-8.0)
- [Model validation](https://learn.microsoft.com/aspnet/core/mvc/models/validation?view=aspnetcore-8.0)
- [Anti-forgery / CSRF](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0)
- [Part 4: controller actions tutorial](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app/controller-methods-views?view=aspnetcore-8.0)

➡️ Next: [Module 07 — Models, ViewModels & validation](../07-models-viewmodels-validation/)
