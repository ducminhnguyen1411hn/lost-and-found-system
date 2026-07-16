# Feature: Unified item board + sidebar filters + merged create  (UI refactor — no FR id)

- **Dimension:** D-CRUD+ re-cut. Not a new business rule — it merges two existing boards (`/FoundItems`, `/LostItems`) into one `/Items`, moves filters into a left sidebar, and merges the two create pages into one form.
- **By:** Dev A   **Status:** Done. Verified at runtime (union counts, filters, paging, redirects, authz, blind listing).
- **Touches:** new `ItemKind` enum · VMs `BoardItemViewModel` / `BoardSearchViewModel` / `ItemCreateViewModel` · `ItemBoardService` (+`IItemBoardService`) · `ItemsController` · Views `Items/Index|Create` + partials `_BoardFilters` / `_BoardItemCard` · `AppTime.Relative` · `_Layout` nav · `Program.cs` (DI + default route) · `FoundItemsController.Index` / `LostItemsController.Index` → 301 redirects.

## A. Lessons & application
- **Business goal:** the old UI had 4 nav items for 2 concepts, a lopsided 3-column filter grid eating the first screen, and cards with 5 equal-weight lines. The app is a *matching exchange*, so found and lost belong on ONE board with a "Loại" filter — seeing both sides together is the product, not a convenience.
- **Decisions worth remembering:**
  - **Merged the UI, not the model.** `FoundItem` and `LostItem` stay separate entities/services/validation. `/Items/Create` maps its VM onto the existing `FoundItemCreateViewModel`/`LostItemCreateViewModel` and calls the existing `CreateAsync` → zero duplicated business logic. Detail/Edit/Delete stay on the old controllers because those flows genuinely differ (claims/handover/PrivateMarks vs resolve).
  - **One `OccurredAt` field** on the merged form, relabelled by kind in JS ("Thời điểm nhặt được" / "Thời điểm mất"), mapped to `FoundAt` or `LostAt` on POST. `HoldingType` + `PrivateMarks` only exist on the found side, so that block is hidden by JS **and** simply ignored when Kind=Lost.
  - **Old boards 301 → `/Items?kind=…`** instead of being deleted: old links keep working, nav stops duplicating.
- **Traps hit (most valuable):**
  1. **`FoundItemStatus.Open = 1` but `LostItemStatus.Open = 0`.** The two "public" filters look identical and are not. Unifying them (or reusing one enum) silently shows `PendingDropoff` found items or hides open lost posts. Each branch filters with its own enum — do not "simplify" this.
  2. **Nothing collection-shaped may sit inside a `Concat` projection.** EF Core can't translate a set operation whose projection contains a collection or correlated subquery (tags, cover-image `FirstOrDefault`, `Count`). Fix: the union row (`BoardRow`) is **scalars only** (column reads + joins); tags and images are hydrated afterwards with small `WHERE Id IN (…)` queries **for the current page's rows only**. Count/OrderBy/Skip/Take stay server-side. A translation failure is a **runtime 500, not a compile error** — the only way to catch it is to actually load the page.
  3. **Razor HTML-encodes `@expression` output to numeric entities** (`ặ` → `&#x1EB7;`), but **literal template text passes through raw UTF-8**. So `grep 'Nhặt được'` on a fetched page finds nothing while `grep 'kết quả'` matches — the first came from an expression, the second from literal markup. Cost ~10 minutes chasing a phantom bug. When verifying Vietnamese output via curl, grep for the ASCII/class names (`rounded-pill`, `stretched-link`) or the entity form — not the display string. (Same family as the FR-FOUND trap about Vietnamese through Git Bash.)
  4. **Stale `dotnet run` holds the exe lock.** A leftover app process makes `dotnet build` fail with `MSB3027`/`MSB3021` (a copy-step failure, *not* a compile error). Kill the process, or build to a temp `-o` dir to check compilation.
- **Reusable where:** any future "one list over two tables" view (e.g. a staff queue mixing claims + camera requests) can copy the scalar-union + hydrate-the-page pattern.

## B. Reusable recipe
- **Steps:** 1) enum + VMs + `AppTime.Relative`. 2) union service (+DI). 3) controller + redirects + default route. 4) views/partials + nav. 5) run and hit every filter combo (the union only proves itself at runtime).
- **Union pattern:** filter each side separately (own Open enum) → project each to the SAME scalar-only row → `Concat` → `Count` → `OrderByDescending(CreatedAt).ThenByDescending(Id)` (stable paging across the union) → `Skip/Take` → hydrate tags/images by id for the page.
- **Validation:** merged VM carries the common annotations (`[Required]`, `[StringLength]`, `[NotInFuture]` on `OccurredAt`); kind-specific rules stay in the per-kind services, which still run untouched.
- **Authorization:** board `[AllowAnonymous]`; create `[Authorize(Roles="Member,Staff,Admin")]` + `[ValidateAntiForgeryToken]`. Board never selects `PrivateMarks` (blind listing by construction).
- **AuditLog / Notification:** unchanged — the existing services still write them; this feature adds no new business event.
- **Verified at runtime:** `/` → 106 = 82 found + 24 lost; `?Kind=Found` → 82; `?Kind=Lost` → 24; page 1 of "Tất cả" genuinely interleaves both kinds; keyword/tag/page filters 200; `/FoundItems`,`/LostItems` → 301; `/Items/Create` anon → 302; old detail pages still 200; type pill renders only on the mixed view (12 vs 0); no `PrivateMarks` in the board HTML.
- **Easy to get wrong when copying:** the two Open enums (trap 1) and putting a subquery in the union projection (trap 2). Also: `ViewData` does flow into a `<partial>`, so the `ShowKindPill` toggle works — but default it to `true` in the partial so it still renders if invoked from elsewhere.

## C. Follow-ups
- Old `FoundItemService.SearchAsync` / `LostItemService.SearchAsync` are now unused (the board no longer calls them). Left in place deliberately — deleting them was out of scope. Remove them if nothing else picks them up.
- Sort is fixed to newest-first; the wireframe shows a "Sắp xếp" control that isn't wired yet.
- Category dropdown is flat-with-parent-prefix (`Cha › Con`), copied from the old create pages for consistency.
