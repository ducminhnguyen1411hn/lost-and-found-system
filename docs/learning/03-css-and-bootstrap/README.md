# Module 03 — CSS & Bootstrap 5

> **Why this matters here:** the project uses **Bootstrap 5** for all styling. You will mostly *apply
> Bootstrap classes*, not write raw CSS. But you need enough CSS to understand what those classes do
> and to nudge things when Bootstrap isn't quite right.

**Time:** ~1.5h · **Prerequisites:** 02 · **Fast track:** ⭐

---

## Part A — Just enough CSS

CSS = rules that style HTML. A rule is **selector + declarations**:

```css
.btn-primary {        /* selector: elements with class "btn-primary" */
  color: white;       /* declaration: property: value; */
  background: #0d6efd;
  padding: 8px 16px;
}
```

**Selectors you'll meet:**
- `.classname` — by class (the common one; Bootstrap is all classes).
- `#id` — by id (rare for styling).
- `element` — by tag, e.g. `table { ... }`.

**The box model** — every element is a box: `content → padding → border → margin`. When spacing looks
wrong, it's almost always padding vs. margin. (Bootstrap gives you classes for these: `p-3`, `m-2`.)

**Layout with Flexbox** — the modern way to put things in a row/column and space them. Bootstrap's
grid and many utilities are built on it:
```css
.row { display: flex; gap: 1rem; }    /* children sit side by side with a gap */
```

**Where CSS lives in this app:** `wwwroot/css/site.css` for your custom tweaks; Bootstrap's CSS is
referenced in `_Layout.cshtml`. Custom CSS should be a thin layer on top of Bootstrap.

📖 [MDN: CSS first steps](https://developer.mozilla.org/en-US/docs/Learn/CSS/First_steps)
· [MDN: The box model](https://developer.mozilla.org/en-US/docs/Learn/CSS/Building_blocks/The_box_model)
· [MDN: Flexbox](https://developer.mozilla.org/en-US/docs/Learn/CSS/CSS_layout/Flexbox)

---

## Part B — Bootstrap 5 (what you'll actually use)

Bootstrap is a library of ready-made CSS classes. You compose them on your HTML elements. Learn these
five groups and you can build every screen in this app.

### 1. The grid (responsive layout)
12 columns per row. Wrap in `.container`, make `.row`s, size columns with `.col-*`:

```html
<div class="container">
  <div class="row">
    <div class="col-md-8">Main content (8/12 wide on medium+ screens)</div>
    <div class="col-md-4">Sidebar (4/12)</div>
  </div>
</div>
```

On small screens columns stack automatically. `md` = the breakpoint (≥768px). That's responsiveness
for free.

📖 [Bootstrap grid](https://getbootstrap.com/docs/5.3/layout/grid/)

### 2. Components
- **Buttons:** `<button class="btn btn-primary">`, `btn-secondary`, `btn-danger`, `btn-outline-*`.
- **Cards** (great for an item in a list):
  ```html
  <div class="card">
    <img src="..." class="card-img-top" alt="...">
    <div class="card-body">
      <h5 class="card-title">Black umbrella</h5>
      <p class="card-text">Found at Library</p>
      <a href="/FoundItems/Details/5" class="btn btn-primary">View</a>
    </div>
  </div>
  ```
- **Tables:** `<table class="table table-striped table-hover">`.
- **Badges** (perfect for a status): `<span class="badge bg-success">Open</span>`,
  `bg-warning`, `bg-secondary`.
- **Alerts** (for messages): `<div class="alert alert-success">Saved!</div>`.
- **Navbar:** the top navigation bar in `_Layout.cshtml`.

📖 [Bootstrap components](https://getbootstrap.com/docs/5.3/components/)

### 3. Forms
Bootstrap styles form controls when you add `form-control` / `form-select`:
```html
<div class="mb-3">
  <label class="form-label">Title</label>
  <input type="text" class="form-control" />
</div>
```
You'll combine these classes with Tag Helpers in Module 09 (`<input asp-for="Title" class="form-control" />`).

📖 [Bootstrap forms](https://getbootstrap.com/docs/5.3/forms/overview/)

### 4. Utilities (spacing, color, text)
Tiny single-purpose classes you stack on anything:
- Spacing: `m-3` (margin), `p-2` (padding), `mt-4` (margin-top), `mb-0`… scale 0–5.
- Text: `text-center`, `text-muted`, `fw-bold`, `fs-5`.
- Color: `text-danger`, `bg-light`. Display: `d-flex`, `d-none`, `gap-2`.

📖 [Bootstrap utilities](https://getbootstrap.com/docs/5.3/utilities/spacing/)

### 5. Icons (optional)
Bootstrap Icons (`<i class="bi bi-bell"></i>`) are handy for a notification bell, etc. Add the
icon stylesheet if you want them.

---

## 🛠️ Exercise

Make a static `.html` file that links Bootstrap from a CDN
(`<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">`)
and build a **found-items listing page**:

1. A `.container` with an `<h1>` and a short intro paragraph (`text-muted`).
2. A `.row` of 3 **cards**, each an item with an image placeholder, title, location, a `badge` status
   (one `bg-success` "Open", one `bg-warning` "Pending", one `bg-secondary` "Returned"), and a
   "View" button.
3. Make the cards sit 3-across on desktop (`col-md-4`) and stack on mobile (resize the window to
   check).
4. Add a Bootstrap `alert-success` at the top saying "1 new match found".

This is essentially the real `FoundItems/Index` page — you'll rebuild it as a Razor view in Module 08.

---

## ✅ Self-check

- [ ] I can explain selector + declaration, and padding vs. margin.
- [ ] I can build a responsive 2- or 3-column layout with the Bootstrap grid.
- [ ] I can make a card, a button, a badge, and an alert with Bootstrap classes.
- [ ] I know spacing utilities (`m-*`, `p-*`) and where to put custom CSS (`wwwroot/css/site.css`).
- [ ] I understand I'll mostly *apply* Bootstrap classes, not write CSS from scratch.

---

## 📚 References

- [MDN: Learn CSS](https://developer.mozilla.org/en-US/docs/Learn/CSS)
- [Bootstrap 5.3 docs](https://getbootstrap.com/docs/5.3/getting-started/introduction/)
- [Bootstrap grid](https://getbootstrap.com/docs/5.3/layout/grid/) ·
  [components](https://getbootstrap.com/docs/5.3/components/) ·
  [forms](https://getbootstrap.com/docs/5.3/forms/overview/)

➡️ Next: [Module 04 — ASP.NET Core fundamentals](../04-aspnetcore-fundamentals/)
