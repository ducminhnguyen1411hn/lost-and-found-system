# Module 08 — Views & Razor

> **Why this matters here:** views are the UI. Razor lets you mix C# into HTML cleanly. Layouts and
> partial views keep the UI DRY — both are required by this project's conventions.

**Time:** ~2h · **Prerequisites:** 02, 03, 07 · **Fast track:** ⭐

---

## 1. Razor syntax in 5 minutes

A `.cshtml` file is HTML with `@` to drop into C#:

```cshtml
@model FoundItemDetailsVm                  @* the type of Model for this view *@

<h1>@Model.Title</h1>                       @* inline expression *@
<p class="text-muted">Found at @Model.LocationName on @Model.FoundAt.ToString("d")</p>

@if (Model.ImagePath is not null)           @* C# control flow *@
{
    <img src="@Model.ImagePath" alt="@Model.Title" class="img-fluid" />
}

<ul>
@foreach (var tag in Model.Tags)            @* loop *@
{
    <li>@tag</li>
}
</ul>

@{                                          @* a code block for local variables *@
    var badgeClass = Model.StatusLabel == "Open" ? "bg-success" : "bg-secondary";
}
<span class="badge @badgeClass">@Model.StatusLabel</span>
```

- `@Model` is the object the controller passed via `return View(vm)`.
- `@model X` (lowercase, top of file) declares the type → you get IntelliSense + compile-time checks.
- Razor **HTML-encodes** `@expressions` automatically → built-in protection against XSS. Don't fight
  it; never hand-build HTML from user input.

📖 [Razor syntax reference](https://learn.microsoft.com/aspnet/core/mvc/views/razor?view=aspnetcore-8.0)

---

## 2. Strongly-typed views (always do this)

Start every view with `@model`. Then `Model` is that type, and a typo like `@Model.Titel` fails the
build instead of at runtime. This pairs with the ViewModel rule from Module 07: the view's `@model` is
always a **ViewModel**, never an entity.

A list view uses a collection model:
```cshtml
@model IEnumerable<FoundItemListItemVm>

@foreach (var item in Model) { /* render a card per item */ }
```

📖 [Views overview: strongly-typed](https://learn.microsoft.com/aspnet/core/mvc/views/overview?view=aspnetcore-8.0)

---

## 3. The three special files

These power the shared look without you repeating markup:

- **`_Layout.cshtml`** (in `Views/Shared/`) — the page chrome: `<head>`, CSS links, the nav bar, the
  footer, and `@RenderBody()` where each view's content is injected. Think "master page."
- **`_ViewStart.cshtml`** — runs before every view; sets `Layout = "_Layout";` so you don't repeat it.
- **`_ViewImports.cshtml`** — shared `@using` namespaces and, importantly,
  `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` which switches on the Tag Helpers you'll use
  in Module 09.

```cshtml
@* _Layout.cshtml (simplified) *@
<!DOCTYPE html>
<html lang="en">
<head>
  <title>@ViewData["Title"] - Lost & Found</title>
  <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
</head>
<body>
  <nav>...</nav>
  <main class="container">
      @RenderBody()        @* each view's content lands here *@
  </main>
  <footer>...</footer>
  @await RenderSectionAsync("Scripts", required: false)   @* views can inject page-specific JS *@
</body>
</html>
```

Set a page title from a view with `@{ ViewData["Title"] = "Found items"; }`.

📖 [Layout in ASP.NET Core](https://learn.microsoft.com/aspnet/core/mvc/views/layout?view=aspnetcore-8.0)

---

## 4. Partial views (`_Name.cshtml`)

A **partial view** is a reusable chunk of markup — required by this repo for repeated UI. Convention:
name it with a leading underscore, e.g. `_FoundItemCard.cshtml`. Render it with the Partial Tag
Helper, passing a model:

```cshtml
@* Views/FoundItems/Index.cshtml *@
<div class="row">
@foreach (var item in Model)
{
    <div class="col-md-4">
        <partial name="_FoundItemCard" model="item" />
    </div>
}
</div>
```

```cshtml
@* Views/FoundItems/_FoundItemCard.cshtml *@
@model FoundItemListItemVm
<div class="card h-100">
  @if (Model.ImagePath is not null) { <img src="@Model.ImagePath" class="card-img-top" alt="@Model.Title"> }
  <div class="card-body">
    <h5 class="card-title">@Model.Title</h5>
    <p class="card-text text-muted">@Model.LocationName</p>
    <a asp-controller="FoundItems" asp-action="Details" asp-route-id="@Model.Id" class="btn btn-primary">View</a>
  </div>
</div>
```

Use partials for: an item card, a tag list, a claim row, the notification dropdown. Use the **layout**
(not a partial) for page-wide chrome.

📖 [Partial views](https://learn.microsoft.com/aspnet/core/mvc/views/partial?view=aspnetcore-8.0)

---

## 5. Blind-listing in the view (a hard rule)

Because the **view** is the last place data is rendered, it's the last place a leak can happen. Two
guarantees:
1. Your ViewModel doesn't even *have* `PrivateMarks` / verification fields (Module 07) — so you can't
   accidentally print them.
2. On public pages and the timeline, only show audit entries where `IsPublic == 1`.

If you ever find yourself writing `@Model.PrivateMarks` in a non-holder view, stop — that's the exact
bug blind-listing exists to prevent.

---

## 🛠️ Exercise

Turn your Module 03 static page into real Razor views for `FR-FOUND`:

1. `Views/FoundItems/Index.cshtml` — `@model IEnumerable<FoundItemListItemVm>`, a Bootstrap `.row`
   that loops and renders a `<partial name="_FoundItemCard" model="item" />`.
2. `Views/FoundItems/_FoundItemCard.cshtml` — the card partial (section 4), with a status `badge`
   whose color depends on the status (use an `@{ }` block).
3. `Views/FoundItems/Details.cshtml` — `@model FoundItemDetailsVm`, showing title, image, category,
   location, found date, status badge. **Confirm there is no way to print `PrivateMarks`.**
4. Set `ViewData["Title"]` in each view and confirm it shows in the browser tab.

---

## ✅ Self-check

- [ ] I can write Razor with `@model`, `@expression`, `@if`, `@foreach`, and a `@{ }` block.
- [ ] I know Razor auto HTML-encodes output (XSS protection) and why that's good.
- [ ] I can explain `_Layout`, `_ViewStart`, `_ViewImports` and what `@RenderBody()` does.
- [ ] I can create and render a partial view with a model.
- [ ] I can state how the view layer enforces blind-listing.

---

## 📚 Microsoft Learn (.NET 8)

- [Razor syntax](https://learn.microsoft.com/aspnet/core/mvc/views/razor?view=aspnetcore-8.0)
- [Views overview](https://learn.microsoft.com/aspnet/core/mvc/views/overview?view=aspnetcore-8.0)
- [Layout](https://learn.microsoft.com/aspnet/core/mvc/views/layout?view=aspnetcore-8.0)
- [Partial views](https://learn.microsoft.com/aspnet/core/mvc/views/partial?view=aspnetcore-8.0)

➡️ Next: [Module 09 — Tag Helpers & forms](../09-tag-helpers-and-forms/)
