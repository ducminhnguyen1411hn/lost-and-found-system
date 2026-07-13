# Feature: Report & public lookup of found items  (FR-FOUND, +FR-TAG, +FR-LOG)

- **Dimension:** D-CRUD+ (create form + public list/detail/search) — the first real vertical slice; it also lays down the shared **Service + AuditLog-in-one-transaction + DI** pattern every later feature copies.
- **By:** Dev A   **Status:** Done (image-upload path wired to Cloudinary; see trap #4 about running the app locally)
- **Touches:** `FoundItem`, `Tag`, `FoundItemTag`, `Category`, `Location`, `AuditLog` · `TagService`, `AuditService`, `CloudinaryImageUploadService`, `FoundItemService` · `FoundItemsController` · Views `Index/Details/Create` + partials `_ItemCard/_Pager/_TagInput` · `SeedData` · `Program.cs` DI.

## A. Lessons & application
- **Business goal:** a finder posts what they found; anyone can browse/search a public list and open a detail page. The backbone every other module builds on (matching/holding/claim all need a `FoundItem`).
- **Decisions worth remembering:**
  - `HoldingType` chosen in the form: **SelfHeld → Open** (public immediately), **Custodial → PendingDropoff** (created but hidden until FR-HOLD staff intake). No staff screen built here.
  - **Verification model = two fields, two sides:** `PrivateMarks` (holder's hidden answer, on FoundItem) vs `VerificationDetails` (claimant's proof, on Claim, later). PrivateMarks is owned by whoever holds the item — reporter (SelfHeld) at posting, staff (Custodial) at intake.
  - Images → **Cloudinary** (`CloudinaryDotNet`), URL stored in `ImagePath`. Upload happens **before** the DB transaction so network I/O doesn't hold a tx open.
- **Traps hit (most valuable):**
  1. **EF store-default sentinel:** the scaffold had `entity.Property(e => e.Status).HasDefaultValue(1)`. EF then treats the CLR default `0` (== `PendingDropoff`, a real Custodial status) as "unset" and omits it on INSERT, so Custodial items silently persisted as `Open`. **Fix:** remove `HasDefaultValue(1)` from `ApplicationDbContext` (the service always sets `Status` explicitly; the DB column default 1 still guards raw inserts). Keep it removed after any re-scaffold.
  2. **Blind-listing must be "by construction," not CSS:** the list VM (`FoundItemListItemViewModel`) has **no** PrivateMarks property and the search projection never selects it; the detail VM fills `PrivateMarks`/`StorageLocation` **only** after the server-side `CanSeePrivate` check. Never `display:none` / hidden input — that leaks via View Source.
  3. **Vietnamese tag normalization can't be tested through Git Bash:** the Windows shell mangles UTF-8 (`í`→byte 0xED) before curl sends it, so tag search "looked broken" (0 matches) when the code was fine. **Verify by pre-percent-encoding UTF-8 yourself** (`Th%E1%BA%BB...`, pure ASCII the shell won't corrupt), or with a unit test. Confirmed: `"Thẻ Sinh Viên"` → stored normalized `"the sinh vien"`, searchable by the accent-free form.
  4. **Pre-existing blocker:** `Program.cs` `AddGoogle(...)` throws `Google ClientId not found` on *every* request when the `Authentication:Google:*` user-secrets are absent (they are, on this machine — only Cloudinary secrets exist) → 500 across the whole app. Not an FR-FOUND bug; documented for follow-up (make Google registration conditional, or add the secrets).
- **Reusable where:** LostAlert create (FR-MATCH), Claim submit (FR-CLAIM), any Admin CRUD — same VM/Service/thin-controller/partial shape.

## B. Reusable recipe
- **Steps (in order):** 1) config/DI (settings + services) → 2) shared services (`TagService`, `AuditService`) → 3) domain service (`FoundItemService`) with the transaction → 4) thin controller → 5) partials → 6) views + nav + `_ViewImports` → 7) seed lookups → 8) build + run + drive the flow.
- **Files:** no schema change (DB-First). ViewModels under `Models/ViewModels/FoundItems/` + `Common/PagedResult<T>` + `Models/Validation/NotInFutureAttribute`. Service pair in `Services/` + `Services/Interfaces/`. Controller thin. Views + `Views/Shared/_ItemCard|_Pager|_TagInput`.
- **Validation:** code = DataAnnotations (`[Required]`, `[StringLength]`, `[NotInFuture]` on FoundAt) + image type/size in the upload service; DB = FK + `CK_FoundItem_Status`/`HoldingType` + `UX_Tag_NormalizedTag`.
- **Authorization:** list/detail `[AllowAnonymous]`; create `[Authorize(Roles="Member,Staff,Admin")]` + `[ValidateAntiForgeryToken]`. Registration grants `Member`. Non-Open items are visible only to reporter/custodian/staff/admin (service returns null → 404 otherwise).
- **AuditLog:** one `"Created"` row per create, in the same transaction; `IsPublic = (status == Open)` (Open=public timeline, PendingDropoff=private).
- **Notification:** none in this slice (no other user involved yet) — arrives with FR-MATCH/FR-CLAIM.
- **Minimum tests (verified):** SelfHeld→Open shows on list; Custodial→PendingDropoff hidden from list + anon detail 404; reporter sees PrivateMarks, anon does not; FoundAt in future rejected; tag search matches on normalized form; AuditLog written with correct `IsPublic`.
- **Easy to get wrong when copying:** the EF store-default sentinel (trap #1) and rendering a hidden field for non-holders (trap #2). Always upload images outside the DB transaction.

## C. Follow-up: Edit + Delete (same feature)
- **Scope:** owner-only (not even Admin), allowed only while `Open`/`PendingDropoff` **and no claim exists** (mirrors the "item with a Pending claim is locked" invariant). `HoldingType` is read-only on edit.
- **Service:** `GetForEditAsync` / `UpdateAsync` / `DeleteAsync` + private `IsEditableAsync`. Update replaces the tag set by **RemoveRange old joins → SaveChanges → add resolved → SaveChanges** (two saves inside the tx to avoid a `UX_FoundItemTag_Item_Tag` clash). New image replaces old; empty keeps old. Delete relies on the `FoundItemTag` cascade FK.
- **Detail VM:** added `CanEdit` (owner && editable && no claim) → drives the Sửa/Xoá buttons.
- **AuditLog:** `"Updated"` is **`IsPublic=true`** (shows "Cập nhật bài đăng" in the timeline — the detail is a generic label, never field values). `"Deleted"` stays `IsPublic=false` (the item is gone, so its timeline is unreachable anyway).
- **Verified at runtime:** owner edit changes title+tags; non-owner (admin) Edit → 404; delete removes item (Details 404, gone from list).
- **Gotcha:** run-lock — if the app is already running, `dotnet build` fails only on the *exe copy* (`MSB3027`); build to a temp `-o` dir to verify compilation without stopping the running instance.

## D. Follow-up: multiple images per post (schema change)
- **DB-First flow (the real one):** edited `db/schema.sql` (added child table `FoundItemImage(Id, FoundItemId, Url, SortOrder)`, migrated the legacy `FoundItem.ImagePath` into it as the cover, dropped the column) → recreated the DB (idempotent `IF NOT EXISTS` + `COL_LENGTH` guards → **no data loss**) → `/db-rescaffold` with `--table FoundItemImage` → copied the new `DbSet`/config into `ApplicationDbContext` → deleted the throwaway `ScaffoldDbContext`.
- **Model:** cover image = the row with the lowest `SortOrder` (0). Create form has **two inputs** — cover (single) + other images (multiple). List returns `CoverImagePath` + `ImageCount` (drives a "📷 N" badge); detail returns the ordered `ImagePaths` array (Bootstrap carousel). Edit = tick-to-remove existing + add-more; on save, kept images are **renumbered** so the cover stays SortOrder 0.
- **Upload:** loop the existing single-file `IImageUploadService.UploadAsync` (cover first, then others) — no interface change. Still uploaded **before** the transaction.
- **Verified at runtime:** create with 1 cover + 2 others → rows at SortOrder 0/1/2, list badge "📷 3", detail carousel of 3; edit-remove the cover → 2 rows renumbered to 0/1 (next image becomes cover), badge "📷 2".
- **Trap:** the re-scaffold + column drop broke every `ImagePath` reference (VMs, service, views, controller `nameof(...ImageFile)`); the compiler lists them all — chase each. Re-running `--force` scaffold regenerated all entities but only `FoundItem` (lost `ImagePath`) + new `FoundItemImage` actually differed.
