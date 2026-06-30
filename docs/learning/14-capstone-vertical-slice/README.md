# Module 14 — Capstone: build one full vertical slice

> **Why this matters here:** this is the graduation exercise. You'll build one real `FR-*` feature
> top-to-bottom — request → controller → service → DB → ViewModel → Razor → form → auth → audit →
> notify — exactly the way you'll build every feature for the rest of the project.

**Time:** ~3h+ · **Prerequisites:** all prior modules · **Fast track:** (this is the finish line)

---

## What "vertical slice" means

A *horizontal* approach builds all the controllers, then all the services, then all the views. A
**vertical slice** builds **one feature through every layer** so it actually works end-to-end. That's
how this project ships (and exactly what the **`/feature-slice`** skill scaffolds). Doing one by hand
first makes the skill make sense.

---

## The slice to build: "Report a found item" (`FR-FOUND`)

Pick the report+list+detail slice. It touches every layer without the complexity of the claim state
machine, so it's the ideal first full feature.

### Layer-by-layer build order

1. **Schema check (DB-First).** Open `db/schema.sql`, confirm the `FoundItem` table has the columns
   you need (Title, Description, CategoryId, LocationId, FoundAt, Status, HoldingType,
   StorageLocation, PrivateMarks, ReporterUserId, CustodianStaffId, …). If you needed a new column you
   would edit `schema.sql` and re-scaffold (Module 10) — for this feature you shouldn't need to.

2. **ViewModels** (`Models/ViewModels/`) — Module 07:
   - `CreateFoundItemVm` (input + Data Annotations + `SelectListItem` lists for Category/Location).
   - `FoundItemListItemVm` (card data, **no** PrivateMarks).
   - `FoundItemDetailsVm` (public detail, **no** PrivateMarks).

3. **Service** (`Services/` + `Services/Interfaces/`) — Module 12:
   - `IFoundItemService` + `FoundItemService` with `CreateAsync`, `GetOpenItemsAsync`,
     `GetDetailsAsync`.
   - `CreateAsync` sets Status = Open (or PendingDropoff if Custodial), normalizes tags via
     `ITagService`, **writes one `AuditLog`** ("Created", `IsPublic` set thoughtfully), single
     `SaveChangesAsync`.
   - Register it `AddScoped` in `Program.cs`.

4. **Controller** (`Controllers/FoundItemsController.cs`) — Modules 06 + 11:
   - `[Authorize]` on the class; `[AllowAnonymous]` on `Index` + `Details` (Guests browse).
   - `Index` → `GetOpenItemsAsync` → `View(list)`.
   - `Details(id)` → `GetDetailsAsync` → `NotFound()` or `View(vm)`.
   - `Create` GET (populate dropdowns) + POST (`[HttpPost]` + `[ValidateAntiForgeryToken]`,
     `ModelState.IsValid` guard, call service with `userManager.GetUserId(User)`, `TempData` success,
     `RedirectToAction(nameof(Details))`).

5. **Views** (`Views/FoundItems/`) — Modules 08 + 09:
   - `Index.cshtml` (loops a `_FoundItemCard` partial in a Bootstrap grid).
   - `Details.cshtml` (status badge; confirm no hidden fields).
   - `Create.cshtml` (Tag-Helper form, validation spans + summary, `_ValidationScriptsPartial`).
   - `_FoundItemCard.cshtml` partial.

6. **Wire & run.** Build, run, log in as a Member, report an item, see it on the list, open its
   details. Then check the DB: a `FoundItem` row **and** an `AuditLog` row both exist.

---

## ✅ Definition-of-Done self-audit

Run your slice against the real DoD (full version in [`CHECKLISTS.md`](../CHECKLISTS.md)). Every box
must be tickable:

- [ ] **Two-tier validation** — Data Annotations on the VM **and** DB constraints in `schema.sql`.
- [ ] **`[Authorize]` + ownership** — public browse vs. login-to-report; (claims later: holder check).
- [ ] **`AuditLog` written** with the correct `IsPublic`.
- [ ] **Legal state transitions** — new item starts Open/PendingDropoff per `HoldingType`.
- [ ] **Tags normalized** — via `ITagService.Normalize`; matching on `NormalizedTag`.
- [ ] **No hidden-field leak** — `PrivateMarks` never reaches a public view/timeline.
- [ ] **TagHelper + PartialView UI** — form uses `asp-*`; card is a partial.
- [ ] **`Notification` sent** when another user is involved (for create, usually none yet — note that).
- [ ] **Happy path + error cases tested** — valid submit; missing Title; bad id → 404; anon → login.
- [ ] **Feature Record written** — run **`/feature-record`** into `docs/features/`.

---

## After the capstone: the real workflow

From here on, you don't build by hand from scratch. The repo gives you power tools:

- **`/feature-slice`** — scaffolds the ViewModel + Service + Interface + thin Controller + Views for a
  new `FR-*`, with the validation/authz/audit/notification wiring stubbed in the right places.
- **`feature-dod-review`** (via `/workflows`) — reviews your working changes against the DoD and
  adversarially verifies each finding before you open a PR.
- **`/feature-record`** — writes the Feature Record (required by the DoD).
- **`/db-rescaffold`** — safe DB-First entity regeneration after a schema change.

Your loop becomes: `/feature-slice` → fill in the logic (using these modules as reference) →
`feature-dod-review` → fix → `/feature-record` → PR. See [`../../INDEX.md`](../../INDEX.md) §13 and
[`../../../CLAUDE.md`](../../../CLAUDE.md).

---

## 🎓 You're done

You can now read and build the LostAndFound codebase: trace a request, write a thin controller, push
logic into a service that audits + notifies in one transaction, query the DB-First way, build
ViewModels + Razor + Tag-Helper forms, enforce auth + ownership, and push realtime notifications.

Keep [`CHECKLISTS.md`](../CHECKLISTS.md) and [`RESOURCES.md`](../RESOURCES.md) open while you work on
real `FR-*` features. Revisit any module when a concept feels shaky — that's what they're for.

---

## 📚 Microsoft Learn (.NET 8) — the end-to-end references

- [Get started with ASP.NET Core MVC (full tutorial)](https://learn.microsoft.com/aspnet/core/tutorials/first-mvc-app/start-mvc?view=aspnetcore-8.0)
- [Develop ASP.NET Core MVC apps (architecture eBook)](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/develop-asp-net-core-mvc-apps)
- The repo's own [`FEATURE_PLAYBOOK`](../../specs/FEATURE_PLAYBOOK.md) — the canonical slice anatomy.
