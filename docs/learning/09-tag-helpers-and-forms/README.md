# Module 09 — Tag Helpers & forms

> **Why this matters here:** this repo mandates **TagHelpers** for forms. They generate correct
> `name`/`id`/validation attributes from your ViewModel, so model binding (Module 07) "just works"
> and you can't typo a field name.

**Time:** ~1.5h · **Prerequisites:** 02, 07, 08 · **Fast track:** ⭐

---

## 1. What a Tag Helper is

A Tag Helper is server-side behavior attached to an HTML element via an `asp-*` attribute. It looks
like HTML, so designers can read it, but ASP.NET expands it at render time. They're enabled by
`@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` in `_ViewImports.cshtml` (Module 08).

📖 [Tag Helpers intro](https://learn.microsoft.com/aspnet/core/mvc/views/tag-helpers/intro?view=aspnetcore-8.0)

---

## 2. The form Tag Helpers you'll use every day

```cshtml
@model CreateFoundItemVm

<form asp-action="Create" method="post">
    <div asp-validation-summary="ModelOnly" class="text-danger"></div>

    <div class="mb-3">
        <label asp-for="Title" class="form-label"></label>
        <input asp-for="Title" class="form-control" />
        <span asp-validation-for="Title" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="CategoryId" class="form-label"></label>
        <select asp-for="CategoryId" asp-items="Model.Categories" class="form-select">
            <option value="">-- choose --</option>
        </select>
        <span asp-validation-for="CategoryId" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="Description" class="form-label"></label>
        <textarea asp-for="Description" class="form-control"></textarea>
        <span asp-validation-for="Description" class="text-danger"></span>
    </div>

    <button type="submit" class="btn btn-primary">Report item</button>
</form>
```

What each does:
- **`<form asp-action="Create">`** → generates the `action` URL **and** a hidden anti-forgery token.
- **`<label asp-for="Title">`** → text from `[Display(Name=...)]` or the property name; `for` matches the input.
- **`<input asp-for="Title">`** → sets `name="Title"`, `id="Title"`, the right `type`, and HTML5
  validation attributes from your Data Annotations. This is the magic that wires the form to your VM.
- **`<select asp-for="CategoryId" asp-items="Model.Categories">`** → a dropdown; `asp-items` takes an
  `IEnumerable<SelectListItem>` you put on the ViewModel.
- **`<span asp-validation-for="Title">`** → shows the error message for that field.
- **`<div asp-validation-summary="ModelOnly">`** → shows model-level (non-field) errors.

📖 [Tag Helpers in forms](https://learn.microsoft.com/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-8.0)

---

## 3. Anti-forgery (CSRF) — it's automatic, keep it

`<form asp-action=...>` injects a hidden `__RequestVerificationToken`. Your POST action validates it
with `[ValidateAntiForgeryToken]` (Module 06). Together they stop a malicious site from submitting
forms as your logged-in user. **Always** keep both halves.

📖 [Prevent CSRF](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0)

---

## 4. Client-side validation (the bonus you get for free)

The `asp-for`/`asp-validation-for` helpers emit `data-val-*` attributes. If the page includes the
jQuery validation scripts, the browser shows errors **before** submitting — fewer round-trips. Add at
the bottom of a form view:

```cshtml
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

Crucial point: client validation is a **convenience**, not security. The server still re-checks with
`ModelState.IsValid`, and the DB still enforces constraints (the two-tier rule). A user can disable
JavaScript; your server and DB don't trust the client.

📖 [Client-side validation](https://learn.microsoft.com/aspnet/core/mvc/models/validation?view=aspnetcore-8.0#client-side-validation)

---

## 5. The anchor Tag Helper (links that survive refactors)

For navigation, generate URLs from controller/action names instead of hardcoding paths:

```cshtml
<a asp-controller="FoundItems" asp-action="Details" asp-route-id="@item.Id" class="btn btn-sm btn-primary">
    View
</a>
```

📖 [Anchor Tag Helper](https://learn.microsoft.com/aspnet/core/mvc/views/tag-helpers/built-in/anchor-tag-helper?view=aspnetcore-8.0)

---

## 6. Reuse forms via a partial

Create/Edit screens usually share fields. Put the fields in `_FoundItemFormFields.cshtml` and render
it from both `Create.cshtml` and `Edit.cshtml`. This satisfies the "reuse via PartialViews"
convention and keeps the two forms in sync.

---

## 🛠️ Exercise

Build the report-item form for `FR-FOUND`:

1. Add to `CreateFoundItemVm` a `List<SelectListItem> Categories` and `List<SelectListItem> Locations`
   (populated in the GET action from the DB).
2. Create `Views/FoundItems/Create.cshtml` with the full form from section 2: Title, Description,
   Category (`select`), Location (`select`), Found date, a checkbox for "Custodial", and a
   validation summary + per-field validation spans. Style with Bootstrap `form-control`/`form-select`.
3. Add the `_ValidationScriptsPartial` in a `@section Scripts` and confirm errors appear client-side
   when you leave Title empty.
4. Submit valid data; confirm it reaches your POST action (set a breakpoint or `TempData` message) and
   that the anti-forgery token is present (view source).
5. Extract the fields into `_FoundItemFormFields.cshtml` and render it from `Create.cshtml`.

---

## ✅ Self-check

- [ ] I can build a form with `asp-action`, `asp-for`, `asp-validation-for`, `asp-validation-summary`.
- [ ] I can build a dropdown with `asp-for` + `asp-items` (`SelectListItem`).
- [ ] I know the anti-forgery token is auto-generated and must be validated server-side.
- [ ] I understand client validation is convenience; server + DB are the real checks.
- [ ] I can generate links with the anchor Tag Helper and reuse a form via a partial.

---

## 📚 Microsoft Learn (.NET 8)

- [Tag Helpers intro](https://learn.microsoft.com/aspnet/core/mvc/views/tag-helpers/intro?view=aspnetcore-8.0)
- [Tag Helpers in forms](https://learn.microsoft.com/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-8.0)
- [Built-in Tag Helpers](https://learn.microsoft.com/aspnet/core/mvc/views/tag-helpers/built-in/?view=aspnetcore-8.0)
- [Anti-forgery](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0)

➡️ Next: [Module 10 — EF Core 8 (DB-First)](../10-ef-core-db-first/)
