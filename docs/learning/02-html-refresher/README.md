# Module 02 — HTML refresher

> **Why this matters here:** your Razor views *are* HTML with a little C# mixed in. Tag Helpers
> (Module 09) generate `<form>`, `<input>`, `<label>`, `<select>` for you — but you have to know what
> those elements are to lay out a page and read what's produced.

**Time:** ~1h · **Prerequisites:** none · **Fast track:** ⭐

Source note: HTML/CSS are web standards, so the canonical reference is **MDN**, not Microsoft Learn.

---

## 1. The skeleton of a page

Every HTML document has the same bones. In this app the *outer* bones live once in
`Views/Shared/_Layout.cshtml`; your individual views only supply the part inside `<body>`.

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" /> <!-- mobile sizing -->
  <title>Lost &amp; Found</title>
  <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
</head>
<body>
  <!-- page content -->
</body>
</html>
```

📖 [MDN: HTML basics](https://developer.mozilla.org/en-US/docs/Learn/Getting_started_with_the_web/HTML_basics)

---

## 2. The elements you'll use constantly

**Text & structure**
- Headings `<h1>`…`<h6>` — one `<h1>` per page; don't skip levels for styling.
- `<p>` paragraph, `<span>` inline wrapper, `<div>` block wrapper (generic boxes for layout).
- Semantic containers: `<header>`, `<nav>`, `<main>`, `<section>`, `<footer>` — same as `<div>` but
  meaningful to screen readers and search engines. Prefer them.

**Lists** (a found-items list is literally this)
```html
<ul>
  <li>Black umbrella — found at Library</li>
  <li>Student card — found at Gate B</li>
</ul>
```

**Tables** (good for an admin list of items)
```html
<table class="table">
  <thead><tr><th>Title</th><th>Found at</th><th>Status</th></tr></thead>
  <tbody>
    <tr><td>Black umbrella</td><td>Library</td><td>Open</td></tr>
  </tbody>
</table>
```

**Links & images**
```html
<a href="/FoundItems/Details/5">View item</a>
<img src="/uploads/item5.jpg" alt="Black umbrella" />   <!-- alt text is required for accessibility -->
```

📖 [MDN: HTML element reference](https://developer.mozilla.org/en-US/docs/Web/HTML/Element)

---

## 3. Forms — the most important part

Almost every feature (report an item, file a claim, subscribe to an alert) is a **form**. Understand
this plain-HTML version first; in Module 09 Tag Helpers will generate it from your ViewModel.

```html
<form method="post" action="/FoundItems/Create">
  <label for="Title">Title</label>
  <input type="text" id="Title" name="Title" />

  <label for="CategoryId">Category</label>
  <select id="CategoryId" name="CategoryId">
    <option value="1">Electronics</option>
    <option value="2">Documents</option>
  </select>

  <label for="Description">Description</label>
  <textarea id="Description" name="Description"></textarea>

  <label><input type="checkbox" name="IsValuable" value="true" /> Valuable item</label>

  <button type="submit">Report item</button>
</form>
```

The pieces that matter:
- **`name`** is the most important attribute — it becomes the key when the form is submitted, and it
  must match your C# property name so **model binding** (Module 07) can fill it in.
- **`method="post"`** sends data in the request body (use for anything that changes data). `get` puts
  values in the URL (use for search/filter).
- **`<label for="...">`** linked to an input's `id` improves accessibility and click targets.
- **`type`** on `<input>` controls the widget + browser validation: `text`, `email`, `number`,
  `date`, `password`, `checkbox`, `hidden`, `file` (for image upload).

📖 [MDN: Your first form](https://developer.mozilla.org/en-US/docs/Learn/Forms/Your_first_form)
· [MDN: The input element](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input)

---

## 4. How this connects to Razor (preview)

In Module 09 you'll write this instead, and ASP.NET generates the HTML above **plus** validation
attributes and an anti-forgery token, with the `name`/`id` guaranteed to match your model:

```cshtml
<form asp-action="Create" method="post">
    <label asp-for="Title"></label>
    <input asp-for="Title" class="form-control" />
    <span asp-validation-for="Title" class="text-danger"></span>
    <button type="submit" class="btn btn-primary">Report item</button>
</form>
```

So: learn the raw HTML now, and Tag Helpers will feel like a shortcut later (because they are).

---

## 🛠️ Exercise

Create a throwaway `.html` file and open it in a browser (no server needed):

1. Build the page skeleton (section 1).
2. Add a `<header>` with an `<h1>` "Lost & Found", and a `<nav>` with two links.
3. Add a `<table class="table">` listing 3 fake found items (Title / Found at / Status).
4. Add the "Report a found item" `<form>` from section 3. Submit it (it'll 404 — that's fine) and
   look at the URL/network tab to see how `name` values are sent.
5. Make one input `required` and one `type="date"`; notice the browser's built-in behavior.

---

## ✅ Self-check

- [ ] I can write the HTML page skeleton from memory.
- [ ] I know why the `name` attribute on inputs is critical (it maps to C# properties).
- [ ] I can build a `<form>` with text, select, textarea, checkbox, and a submit button.
- [ ] I know the difference between `method="get"` and `method="post"` and when to use each.
- [ ] I can build a `<table>` and a `<ul>` to display a list of items.

---

## 📚 References (MDN)

- [Learn HTML (structured course)](https://developer.mozilla.org/en-US/docs/Learn/HTML)
- [HTML forms guide](https://developer.mozilla.org/en-US/docs/Learn/Forms)
- [HTML element reference](https://developer.mozilla.org/en-US/docs/Web/HTML/Element)

➡️ Next: [Module 03 — CSS & Bootstrap 5](../03-css-and-bootstrap/)
