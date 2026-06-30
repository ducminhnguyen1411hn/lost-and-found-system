# Checklists & quick references

Keep this open while you build. Two parts: the **Definition of Done** as a working checklist, and a
fast **"how do I render a view"** cheat-sheet.

---

## 1. Definition of Done — per feature

Copy this into your PR description and tick every box. (Source: [`../../CLAUDE.md`](../../CLAUDE.md) +
[`../INDEX.md`](../INDEX.md) §9.) The module that teaches each item is in brackets.

- [ ] **Two-tier validation** — Data Annotations / service checks in code **and** real DB constraints
      in `schema.sql`. *(Module 07)*
- [ ] **Correct `[Authorize]`** — right roles / `[AllowAnonymous]` for public pages. *(Module 11)*
- [ ] **Ownership check** — per-record actions verify the holder/owner, not just the role. *(Module 11)*
- [ ] **`AuditLog` written** — one row per status change, with the **right `IsPublic`**. *(Module 12)*
- [ ] **Legal state transitions** — guarded; illegal transitions rejected. *(Module 12)*
- [ ] **Tags normalized** — via `ITagService.Normalize`; matching/subscribe compare on
      `NormalizedTag`. *(Module 12)*
- [ ] **No hidden-field leak** — `PrivateMarks` / claim verification never on public pages or the
      timeline (only `IsPublic = 1` audit rows). *(Modules 07–08)*
- [ ] **TagHelper + PartialView UI** — forms use `asp-*`; repeated UI is a partial. *(Modules 08–09)*
- [ ] **`Notification` sent** — whenever another user is involved (store + live push). *(Modules 12–13)*
- [ ] **Happy path AND error cases tested** — valid input; invalid input; not-found; unauthorized.
- [ ] **Feature Record written** — via **`/feature-record`** into `docs/features/`. *(required)*

### State-machine invariants (don't break these)
- [ ] At most **one** `Accepted` claim per item.
- [ ] Accepting a claim **auto-rejects** the other pending claims on that item (same transaction).
- [ ] A `Pending` claim **locks** the item.
- [ ] `FoundItem` → `Returned` only when **both** `HolderConfirmedHandover` **and**
      `ClaimantConfirmedHandover` are true.
- [ ] Every status change writes exactly **one** `AuditLog` row.

### DB-First guardrails
- [ ] Schema changed in `db/schema.sql` **first** (not in C#).
- [ ] Entities regenerated via **`/db-rescaffold`**; `Models/Entities/` not hand-edited.
- [ ] **No** EF migrations run (`migrations add` / `database update`).
- [ ] Locked enums in `Models/Enums/` not renamed/reordered.

---

## 2. View-rendering quick reference

The bits you'll forget and look up constantly.

### View skeleton
```cshtml
@model FoundItemDetailsVm            @* strongly-typed: always a ViewModel, never an entity *@
@{ ViewData["Title"] = "Item details"; }

<h1>@Model.Title</h1>
@if (Model.ImagePath is not null) { <img src="@Model.ImagePath" class="img-fluid" alt="@Model.Title" /> }
```

### Razor control flow
```cshtml
@if (cond) { <p>yes</p> } else { <p>no</p> }
@foreach (var x in Model.Items) { <li>@x.Name</li> }
@{ var cls = x.IsOpen ? "bg-success" : "bg-secondary"; }   @* code block for locals *@
```

### A form (Tag Helpers)
```cshtml
<form asp-action="Create" method="post">
    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
    <div class="mb-3">
        <label asp-for="Title" class="form-label"></label>
        <input asp-for="Title" class="form-control" />
        <span asp-validation-for="Title" class="text-danger"></span>
    </div>
    <select asp-for="CategoryId" asp-items="Model.Categories" class="form-select"></select>
    <button type="submit" class="btn btn-primary">Save</button>
</form>
@section Scripts { <partial name="_ValidationScriptsPartial" /> }   @* client-side validation *@
```

### Links & partials
```cshtml
<a asp-controller="FoundItems" asp-action="Details" asp-route-id="@item.Id" class="btn btn-sm btn-primary">View</a>
<partial name="_FoundItemCard" model="item" />
```

### Bootstrap pieces you reuse
```cshtml
<span class="badge bg-success">Open</span>          @* bg-warning / bg-secondary / bg-danger *@
<div class="alert alert-success">Saved!</div>
<div class="row"><div class="col-md-4">…</div></div> @* 3-across on desktop, stacks on mobile *@
<div class="card"><div class="card-body">…</div></div>
```

### TempData success message after redirect (Post/Redirect/Get)
```csharp
// controller
TempData["Success"] = "Item reported.";
return RedirectToAction(nameof(Details), new { id });
```
```cshtml
@* _Layout.cshtml or the view *@
@if (TempData["Success"] is string msg) { <div class="alert alert-success">@msg</div> }
```

---

## 3. Controller action quick reference
```csharp
return View(vm);                          // render the matching view with a ViewModel
return RedirectToAction(nameof(Index));    // 302 (use after a successful POST)
return NotFound();                         // 404
return Forbid();                           // 403 — logged in but not allowed (failed ownership)
if (!ModelState.IsValid) return View(vm);  // tier-1 validation failed → redisplay with errors
```

```csharp
[HttpPost]
[ValidateAntiForgeryToken]                 // always on POST
public async Task<IActionResult> Create(CreateFoundItemVm vm) { ... }
```

---

## 4. Current-user quick reference
```csharp
string? userId = userManager.GetUserId(User);   // inject UserManager<ApplicationUser>
bool isStaff   = User.IsInRole("Staff");
bool signedIn  = User.Identity?.IsAuthenticated == true;
```

---

See [`README.md`](README.md) for the full module path and [`RESOURCES.md`](RESOURCES.md) for every
documentation link.
