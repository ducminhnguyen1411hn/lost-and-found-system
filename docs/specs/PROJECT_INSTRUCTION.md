# School Lost-and-Found Management System — Project Instruction

> Development orientation & conventions document. Every architectural, business, and convention decision is recorded here. Read this file before writing a single line of code.

---

## 1. Product positioning

This is **not** an "online storage box". This is a **matching exchange for lost/found information, tied to a transparent return workflow**.

Philosophy after re-review:
- **Trust the finder.** By default the finder self-holds the item and returns it directly to the owner — fast, with no staff bottleneck. A bad actor would have simply walked off with it from the start rather than posting it.
- **Staff only step in when needed:** holding high-value items on someone's behalf, or arbitrating when there is a dispute.
- **Leave a trail, no excess control.** After handover, both sides confirm to close the lifecycle — enough to trace, without slowing anyone down.

The core value lies in **3 things**, not CRUD:
1. **Watch-alert style matching** — the searcher subscribes a filter, and when a new matching item appears the system notifies automatically.
2. **Return process with verification** — a clear lifecycle, preventing mistaken returns/disputes.
3. **Realtime notification** — notify the right person, at the right time.

---

## 2. Tech stack (mandatory)

| Item | Technology |
|---|---|
| Framework | ASP.NET Core MVC, **.NET 8 (LTS)** — fixed, do not use another version |
| ORM | Entity Framework Core (Code First + Migrations) |
| Database | SQL Server |
| Authentication & authorization | ASP.NET Core Identity (role-based) |
| Realtime | SignalR |
| Frontend | HTML5, CSS3, Bootstrap 5 |
| View | Razor + **TagHelper** + **PartialView** |
| Business logging | `AuditLog` table in the DB + (optional) Serilog for technical logging |

Architecture conventions:
- **Thin controller** — orchestration only. Business logic lives in the **Service** tier.
- Every important business action goes through a Service → the Service is responsible for: handling the business rule + writing `AuditLog` + pushing `Notification`.
- Do not pass an Entity straight to a View — use a **ViewModel**.

---

## 3. Actors & authorization (Identity)

| Role | Description | Main permissions |
|---|---|---|
| `Guest` (not logged in) | Casual visitor | View the public found-item list + detail. Cannot post, cannot claim. |
| `Member` | Student / Lecturer | Subscribe watch-alerts (lost item), report a found item, claim, **approve/reject claims on items they are holding**, confirm handover, thank the finder, request a camera check. |
| `Staff` | Security / Proctor | Receive & hold custodial items, process claims for stored items, **arbitrate disputes**, process camera-check requests. |
| `Admin` | Administrator | Manage users & roles, categories (2-level Category) & Location & Tag, handle unclaimed items, dashboard, view logs. |

- Authorization via `[Authorize(Roles = "...")]` at the Controller/Action level.
- The right to approve a claim depends on **who is currently holding the item** (`HoldingType`), not hardcoded by role — see section 5.
- Seed **1 sample account per role** when initializing the DB.

---

## 4. Data model

### Main entities

- **ApplicationUser** : inherits `IdentityUser`. Adds `FullName`, `StudentOrStaffCode`, `Department`, `PhoneNumber`. (Optional: `ThankCount`, `ReturnedCount` for reputation — can be computed dynamically.)
- **Category** : a **2-level, system-predefined** taxonomy. Has `ParentId` (self-reference). Users only **pick**, they do not create their own.
- **Location** : a place on campus (`Building`, `Name`).
- **LostAlert** : a Member's **watch-alert subscription** (publish/subscribe) — replacing the old-style "lost report".
- **FoundItem** : a found item + its public post. Lifecycle is distinguished by `Status`; the holding place is distinguished by `HoldingType`.
- **Tag** / **FoundItemTag** : keyword labels (many-to-many with FoundItem), storing both the display form and the normalized form.
- **Claim** : a request to recover a `FoundItem`.
- **CameraCheckRequest** : a request for security to extract camera footage.
- **ThankYou** : a thank-you message + rating of the finder after a successful return.
- **Notification** : a notification sent to a user (stored permanently).
- **AuditLog** : the business log (also the data source for the public timeline).

### Relationship summary

```
ApplicationUser 1───* LostAlert
ApplicationUser 1───* FoundItem      (finder / holder)
ApplicationUser 1───* Claim          (the claimant)
ApplicationUser 1───* CameraCheckRequest
ApplicationUser 1───* Notification
Category (ParentId self-reference)  1───* LostAlert / FoundItem
Location        1───* LostAlert / FoundItem / CameraCheckRequest
FoundItem       *───* Tag            (via FoundItemTag)
FoundItem       1───* Claim
FoundItem       1───1 ThankYou       (after Returned)
```

### Three groups of fields — kept distinct by purpose

| Group | Fields | Purpose |
|---|---|---|
| **Structured** | `CategoryId`, `LocationId`, `FoundAt`, `Tags` (normalized) | **Matching / subscribe** — exact machine comparison |
| **Free-form** | `Title`, `Description` | **LIKE search** for casual browsers — NOT used for matching |
| **Hidden** | `PrivateMarks` | **Verifying the true owner** — only the holder / staff can see it |

### Important fields

`FoundItem`: `Id, Title, Description, CategoryId, LocationId, FoundAt, Status, HoldingType, StorageLocation(nullable), ImagePath, PrivateMarks(hidden), ReporterUserId, CustodianStaffId(nullable), HolderConfirmedHandover(bool), ClaimantConfirmedHandover(bool), CreatedAt`.

`LostAlert`: `Id, OwnerUserId, CategoryId(nullable), LocationId(nullable), FromDate(nullable), ToDate(nullable), Keyword(nullable), IsActive, CreatedAt`. (Watched tags are stored in the join table `LostAlertTag`.)

`Claim`: `Id, FoundItemId, ClaimantUserId, VerificationDetails, EvidenceImagePath(nullable), Status, HandledByUserId(nullable), RejectReason(nullable), CreatedAt, HandledAt(nullable)`.

`Tag`: `Id, DisplayTag, NormalizedTag(UNIQUE)`.

`CameraCheckRequest`: `Id, RequesterUserId, LocationId, FromTime, ToTime, ItemDescription, Status, HandledByStaffId(nullable), ResponseNote(nullable), CreatedAt, HandledAt(nullable)`.

`ThankYou`: `Id, FoundItemId, FromUserId, ToUserId, Rating(1-5), Message, CreatedAt`.

`AuditLog`: `Id, ActorUserId, Action, EntityType, EntityId, FromStatus, ToStatus, Detail, IsPublic(bool), CreatedAt, IpAddress`.

`Notification`: `Id, RecipientUserId, Type, Title, Message, LinkUrl, IsRead, CreatedAt`.

---

## 5. Lifecycle (state machine) — the "soul" part

### HoldingType (where the item is held)
- `SelfHeld` (**default**) — the finder self-holds and returns it directly.
- `Custodial` — handed to staff to hold (usually for valuable/sensitive items).

**The "holder"** = the finder if `SelfHeld`, = staff if `Custodial`. The holder is the one who approves claims & performs the handover. In a **dispute**, staff can take over the arbitration.

### FoundItem

```
Create post:
  SelfHeld  ──────────────────────────► [Open]
  Custodial ─► [PendingDropoff] ─(staff receives the item)─► [Open]

[Open]  (public, claimable)
   │ holder accepts 1 claim
   ▼
[ClaimAccepted]  (recipient chosen, awaiting handover)
   │ holder confirms "handed over"  +  claimant confirms "received"  (BOTH)
   ▼
[Returned] ✓  (closes the lifecycle, opens the thank-you flow)

[Open]         ──past 30/60 days──► [Unclaimed] ──► [Disposed]   (mainly for Custodial items)
[ClaimAccepted] ──holder cancels───► [Open]                       (reopen)
```

### Claim

```
[Pending] ──holder accepts──► [Accepted]  (other claims on the same item automatically → Rejected)
[Pending] ──holder rejects───► [Rejected]  (with RejectReason)
```

**Invariant rules (invariants):**
- A `FoundItem` has **at most 1 claim in the Accepted state**.
- When an item has a `Pending` claim → **gather all claims**, do not accept hastily (anti-dispute).
- Move to `Returned` only when **both `HolderConfirmedHandover` and `ClaimantConfirmedHandover` are true**.
- Every `Status` change → produces **1 AuditLog record**.
- Do not allow `FoundAt` in the future; `LostAlert.FromDate ≤ ToDate`.

---

## 6. Business functions by actor

### Member
- **Report a found item** → create a `FoundItem`. Choose `HoldingType` (default self-held). Enter public fields + hidden `PrivateMarks` + `Tags`.
- **Subscribe a watch-alert (LostAlert)**: choose area / time / category / tags to get notified when a matching item appears.
- **Claim** a `FoundItem` with verification details (+ evidence photo if any).
- If the **holder**: approve / reject claims on their item; confirm the handover.
- If the **recipient**: confirm the item was received.
- **Thank** the finder after receiving the item (rating + thank-you message).
- Send a **camera-check request**. Receive notifications (bell + realtime).

### Staff
- Receive & hold **Custodial items**: confirm `PendingDropoff` → `Open`, record the storage location.
- Approve / reject claims for stored items; confirm the handover of stored items.
- **Arbitrate disputes** when several people claim the same item.
- Process **camera-check requests**: accept them, respond with the result (found / not found / need more information).

### Admin
- Manage users, assign roles.
- Manage **Category (2-level)**, **Location**, **Tag** (merge/clean up junk tags).
- Handle **overdue unclaimed items** (`Unclaimed` → `Disposed`).
- **Dashboard**: number of found posts, successful return rate, average return time, longest-held item, top finders.
- View the **AuditLog**.

---

## 7. Matching — watch-alert style (publish/subscribe)

Invert the logic for simplicity: **do not scan back over all lost reports**. Instead, when a `FoundItem` transitions to `Open` → the system checks which active (`IsActive`) `LostAlert`s it matches → fires a notification to the owners of those alerts.

Match criteria (using only **structured** fields):
- `Category` matches (or shares the same parent group) — if the alert specified one.
- `Location` matches/is near — if the alert specified one.
- `FoundAt` falls within `[FromDate, ToDate]` — if the alert specified one.
- **Tag** matches (compared on `NormalizedTag`) — if the alert specified one.

Combination rule: **AND across criteria types**, **OR within the same tag group** (hitting one tag counts). Any criterion the user left blank is skipped. Simple enough — no scoring algorithm needed.

The active direction: a Member can still **browse + search + filter** the public found-item list at any time.

---

## 8. Tags & normalization (MANDATORY to share 1 function)

A tag is the filter layer that sits **between** Category (fixed) and Description (free-form): loose labels the poster adds themselves ("móc khóa" (keychain), "vòng tay" (bracelet), "hình mèo" (cat picture)) — things not worth a dedicated category but that you still want to filter/subscribe on.

**Normalization pipeline** (run identically for both posting an item and subscribing a watch-alert):
1. Trim leading/trailing whitespace.
2. `ToLower()` the whole string.
3. **Strip Vietnamese diacritics** (`string.Normalize(NormalizationForm.FormD)` then filter out diacritic/CombiningMarks characters — standard .NET, no external lib needed).
4. Collapse extra whitespace into a single space.
5. (optional) remove special characters, keeping only letters-digits-spaces.

Result: `"Móc Khóa "`, `"MÓC KHÓA"`, `"móc  khóa"` → all become `moc khoa`.

**Store both forms:** `DisplayTag` (original, for display) + `NormalizedTag` (for matching & subscribing). Display uses Display; comparison ALWAYS uses Normalized.

> Hard convention: write **one** single `TagService.Normalize(string)` function, called by both devs. A divergent normalizer = wrong matches.

**Storage:** the `Tag` table (`NormalizedTag` UNIQUE) + a many-to-many join table. (A faster option if the deadline is tight: a single string column `NormalizedTags` then LIKE — but a separate table is cleaner and makes autocomplete easier later.)

*Nice to have if there is time:* tag autocomplete while typing (drop down existing tags to reuse → data naturally consolidates into one place).

---

## 9. Owner verification — "blind listing"

Principle: **hide some identifying information** on the public post, so that only the true owner can describe it.

- **Public** (everyone can see): item type, color, place/time found, photo with details obscured. E.g.: "Black leather wallet, found near the library".
- **Hidden** (`PrivateMarks`, only the holder/staff can see): characteristics only the owner knows. E.g.: what is inside the wallet, the phone wallpaper, which corner is scratched.

When claiming, the claimant must **describe the hidden part correctly**. The holder places the description next to `PrivateMarks` to grade it. The verification methods, ordered by strength:
1. **Describe the hidden characteristics** — strongest, applies to every item.
2. **Proof of ownership** — old photos in use, receipts, serial number (the `EvidenceImagePath` field in Claim).
3. **Automatic identity match** — for named items (student card/documents): compare the name/code with the claimant's account → almost automatically suggests the owner.
4. **Dispute** — several claimants → staff compare the evidence, pick the best match, and log it transparently.

---

## 10. Notification + SignalR

**Pattern: DB-backed + realtime push.** A business event occurs → the Service writes 1 `Notification` record → and simultaneously pushes it via the SignalR Hub to the online user/group. If offline, the next login still shows the red bell.

Realtime push events:

| Event | Recipient |
|---|---|
| A new item matches your watch-alert | Member (the LostAlert owner) |
| Someone claimed an item you are holding | Holder (finder / staff) |
| A claim was accepted / rejected | Member (the claimant) |
| The other party confirmed the handover | The other side |
| A new camera-check request | Staff (group `"staff"`) |
| A response to a camera-check request | Member (the requester) |
| You received a thank-you | Member (the finder) |

**SignalR groups:** by `userId` (personal) and `"staff"` (the shared staff dashboard).

---

## 11. Public event timeline (in the post detail)

A `FoundItem`'s detail page shows a **timeline** built from `AuditLog`, filtered by that item's `EntityId` and **taking only `IsPublic = true` records**.

```
● 12/06 09:15  An reported a found item (near the library)
● 12/06 14:02  Bình submitted a recovery request
● 13/06 08:30  An accepted → recipient chosen
● 13/06 16:40  Handover — both confirmed ✓ Returned
```

- The timeline shows **only** safe events (status changes, who returned to whom). It does **not** leak `PrivateMarks`, `VerificationDetails` content, or evidence photos — those are viewable only by the parties involved/staff.
- On `Returned`: close the timeline with a result line; the top of the post attaches the thank-you/rating (if any).

---

## 12. Data constraints — validate at BOTH tiers

### Code tier (server-side)
- Data Annotations on the ViewModel: `[Required]`, `[StringLength]`, `[Display]`, `[RegularExpression]`, custom `ValidationAttribute` (e.g.: date not in the future).
- `ModelState.IsValid` in the controller.
- Business checks in the Service (e.g.: do not allow claiming an item that is not in the `Open` state; do not allow moving to `Returned` when a confirmation is missing).

### Database tier (Fluent API → real constraints)
- `NOT NULL` for required fields; `MaxLength` for strings.
- `UNIQUE`: `StudentOrStaffCode`, `Tag.NormalizedTag`.
- FK + a sensible `DeleteBehavior` (avoid cascade-deleting history by mistake — especially `AuditLog`, `Claim`).
- `CHECK` for the status enum / `Rating` (1–5) if needed.

> The client (Bootstrap) is for UX only. **The server and the DB are the real gatekeepers.**

---

## 13. Business logging

- **AuditLog (DB table)** — the main requirement. Record every important action: item status change, create/accept/reject claim, handover + 2 confirmations, handling unclaimed items, role assignment, processing camera requests… Each record: who, what action, which entity, from which status → to which, when, `IsPublic`?.
- *(optional)* **Serilog / ILogger** — technical logging (exception, request) to a file. Bonus points, not required.

The AuditLog write sits inside the Service, **in the same transaction** as the business action.

---

## 14. Project structure & conventions

```
/Controllers          # thin, orchestration only
/Models
    /Entities         # EF Core entities
    /ViewModels       # models for the View
/Data
    AppDbContext.cs
    /Migrations
    DbSeeder.cs       # seed roles + sample accounts + 2-level Category + sample Locations
/Services             # ItemService, ClaimService, MatchingService, NotificationService,
    /Interfaces       #   TagService, AuditService, CameraRequestService, ThankYouService...
/Hubs                 # SignalR Hub
/Views
    /Shared           # _Layout + PartialView (_Header, _Footer, _NotificationBell,
                      #   _ItemCard, _Timeline, _Pager, _TagInput, _ClaimForm...)
/TagHelpers           # custom TagHelper if any
/wwwroot              # css, js, uploaded images
```

Conventions:
- ViewModel suffix `...ViewModel` / `...Vm`.
- A Service returns a `Result` or throws a clear business exception; the controller catches it → pours it into `ModelState`/`TempData`.
- PartialView names start with `_`.
- Forms split into PartialViews; use TagHelpers (`asp-for`, `asp-validation-for`, `asp-action`...) instead of hand-written HTML.
- **Every finished feature → create 1 Feature Record** per `FEATURE_PLAYBOOK.md` (lessons + reusable recipe). Mandatory, do not pile them up at end of term.

---

## 15. 6-week roadmap

| Week | Goal | Content |
|---|---|---|
| **1** | Foundation | Set up .NET 8, EF Core + SQL Server, Identity with 4 roles, design the full-entity DB (including Tag/LostAlert/Camera/ThankYou), seed roles + accounts + **2-level Category**. Layout + Bootstrap 5, skeleton PartialViews. Lock down the **state machine** + **enums** + the shared **tag normalization function**. |
| **2** | Core CRUD | Report found (public fields + PrivateMarks + Tags), public list + detail, LIKE search + filter (category/location/tag) + pagination. TagHelper for forms. |
| **3** | Holding + permissions | SelfHeld vs Custodial flow (Staff receive stored items). Strict `[Authorize]`. Image upload. Start writing AuditLog + build the **public timeline**. |
| **4** ⭐ | Claim + Matching + SignalR | Claim → accept/reject → handover with **2-way confirmation**. LostAlert (subscribe) + publish/subscribe matching. Notification (DB) + SignalR for the main events. |
| **5** | Auxiliary modules + Admin | CameraCheckRequest, ThankYou/Rating, Dashboard, handling unclaimed items, the log-viewing page, Tag management. Claim disputes. |
| **6** | Buffer | Polish the UI, fill in validation, test the flows, tag autocomplete (if there is time), write the report/slides. |

**Feature tiering:**
- **Must have:** publish/subscribe matching + DB notification + SignalR (matching item, claim accept/reject) + 2-way handover confirmation + public timeline.
- **Nice to have if time permits:** claim disputes, CameraCheckRequest, ThankYou, tag autocomplete.
- **Optional:** finder reputation/leaderboard, near-expiry warnings, bulk stock-in.

---

## 16. Definition of Done (per function)

- [ ] Validation covers both tiers (code + DB).
- [ ] Authorization is correct, tested with an account lacking permission.
- [ ] Important actions write to AuditLog (with `IsPublic` set correctly).
- [ ] Status changes follow the state machine; do not move to `Returned` when a confirmation is missing.
- [ ] Tag/subscribe compared on the normalized form (the same function).
- [ ] No hidden fields leaked (`PrivateMarks`, verification content) to public places.
- [ ] UI uses TagHelper + PartialView, responsive Bootstrap 5.
- [ ] A notification is sent if the event involves another user.
- [ ] Test both the happy path and error cases (claim in the wrong state, locked item, dispute…).

---

## 17. Getting started

> **Requirement:** .NET 8 SDK. Check `dotnet --list-sdks` (must have an `8.x.x` line). Every EF Core/Identity/SignalR package uses the **8.x** line.

```bash
# 1. Create the project (force the net8.0 framework)
dotnet new mvc -n LostAndFound -f net8.0
cd LostAndFound

# 2. Install packages (pin version 8.x)
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.*
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.*
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.*
# SignalR server ships with ASP.NET Core; the client side uses the JS library

# 3. Configure the ConnectionString in appsettings.json

# 4. First migration
dotnet ef migrations add InitialCreate
dotnet ef database update

# 5. Run
dotnet run
```

After the DB is created, `DbSeeder` auto-seeds: 4 roles, 1 account per role, 2-level Category + sample Locations.
