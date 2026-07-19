# Feature: Lost-item board (FR-LOST)

- **Dimension:** full mirror of FR-FOUND for the *active* lost side — a public "Đồ bị mất" board so a loser can proactively post, instead of only the passive watch-alert (LostAlert) the base spec had.
- **By:** Dev A   **Status:** Done (matching/notify deferred to FR-MATCH/FR-NOTI)
- **Touches:** new `LostItem` + `LostItemImage` + `LostItemTag` (DB-First) · enum `LostItemStatus` · `LostItemService` · `LostItemsController` · Views `Index/Details/Create/Edit` + `_LostItemCard`/`_LostPager` · `SeedDemoData.SeedLostItemsAsync`.

## A. Lessons & application
- **Why:** the pure watch-alert model makes the loser passive (depends on finders posting). A public lost post gives agency AND still feeds future matching. User chose a *separate* board (vs unifying into LostAlert).
- **Scope decision:** images = full mirror (cover + others, optional). Matching (query + notify) explicitly deferred — this slice is just the board (CRUD + list/detail/search + resolve).
- **Simplifications vs FoundItem:** no `PrivateMarks` / `HoldingType` (the owner just describes their own item; no verification/holder concept). Statuses are `Open/Resolved/Cancelled`; edit/delete/resolve allowed only while `Open`.
- **Reused wholesale:** `TagService`, `AuditService`, `AppTime` (UTC), Cloudinary upload, the multi-image cover/others + ×-remove pattern, date-range filter, blind-listing discipline (though lost posts have no hidden fields).
- **Traps:** the additive schema (`IF OBJECT_ID IS NULL`) means recreating the DB is unnecessary — re-run `schema.sql` and it creates only the new tables, keeping the 100 seeded found items. `--force` re-scaffold also touched `Category/Location/Tag` (they gained nav collections to the new tables) — expected.

## B. Reusable recipe
- **Steps:** schema.sql (3 tables, additive) → apply (keeps data) → `/db-rescaffold` (+3 `--table`) → copy config into `ApplicationDbContext` → enum → VMs → service → DI → controller → views + partials → nav + `_ViewImports` → seed → build + run + verify.
- **Auth:** list/detail `[AllowAnonymous]`; create/edit/delete/resolve `[Authorize]` (+ owner check in the service). Non-Open posts hidden from the public (service returns null → 404).
- **AuditLog:** `Created`/`Updated`/`Resolved` are `IsPublic=true` (timeline); `Deleted` is `false`. `EntityType="LostItem"`.
- **Verified:** 24 seeded (Open); create→owner buttons; admin (non-owner) Edit→404; Resolve→Status=Resolved, hidden from list + anon detail 404; found data intact.
- **Easy to get wrong:** forgetting the owner check is server-side (not just hiding buttons); and the resolved/cancelled visibility gate.
