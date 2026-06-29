# Requirements & 2-Developer Work Split — Lost & Found Management System

> Updated to reflect the latest decisions. Items are numbered `FR-*` for easy traceability and assignment. Read together with `PROJECT_INSTRUCTION.md`.

---

## 1. Work-split principles

- Split by **vertical slice**: each person owns a module end to end, from Entity → Service → Controller → View. Avoid a horizontal "backend/frontend" split.
- **Week 1 is shared work** on the foundation (DbContext, entities, Identity, layout, **finalizing the state machine + enums + the tag normalization function**) before branching off.
- Both sides agree up front on the **shared interfaces** (Section 5) so parallel coding does not block either party.
- Each person is responsible for their own module's two-tier validation + AuditLog + Notification.

---

## 2. Functional Requirements

### Authentication & authorization — `FR-AUTH`
| ID | Description |
|---|---|
| FR-AUTH-01 | Register a Member account (FullName, student/staff code, faculty, phone number, email, password). |
| FR-AUTH-02 | Sign in / sign out via Identity. |
| FR-AUTH-03 | Authorization for 4 roles: Guest / Member / Staff / Admin using `[Authorize(Roles=...)]`. |
| FR-AUTH-04 | Seed 4 roles + 1 sample account per role + a 2-level Category + sample Location records. |
| FR-AUTH-05 | Admin views the user list and grants / revokes roles. |

### Reporting found items & public lookup — `FR-FOUND`
| ID | Description |
|---|---|
| FR-FOUND-01 | A Member reports a found item → creates a `FoundItem`. Choose `HoldingType` (default `SelfHeld`). |
| FR-FOUND-02 | Enter data separately: public fields + `PrivateMarks` (hidden) + `Tags`. |
| FR-FOUND-03 | Guest/Member views the public list + public detail (PrivateMarks hidden). |
| FR-FOUND-04 | LIKE search (Title/Description) + filter by Category/Location/Tag + pagination. |
| FR-FOUND-05 | Upload an item image (stored in `wwwroot`, keep `ImagePath`). |

### Tags & normalization — `FR-TAG`
| ID | Description |
|---|---|
| FR-TAG-01 | A single `TagService.Normalize` function: trim + lower + strip Vietnamese diacritics + collapse spaces. Shared. |
| FR-TAG-02 | Store `Tag(DisplayTag, NormalizedTag UNIQUE)` + a many-to-many join table with FoundItem. |
| FR-TAG-03 | Display uses Display; all comparison/subscription uses Normalized. |
| FR-TAG-04 | *(nice to have)* Tag autocomplete while typing (drop down existing tags so they can be reselected). |

### Watch-alert subscriptions & Matching — `FR-MATCH`
| ID | Description |
|---|---|
| FR-MATCH-01 | A Member creates a `LostAlert`: choose area / time / category / tags (skip any left blank). |
| FR-MATCH-02 | When a `FoundItem` moves to `Open` → check for matches against the active `LostAlert` records. |
| FR-MATCH-03 | Matching rule: AND across criteria types, OR within the same tag group (Normalized). |
| FR-MATCH-04 | On a match → fire a notification (DB + SignalR) to the LostAlert owner. |
| FR-MATCH-05 | A Member toggles (`IsActive`) on/off or deletes their own watch-alert subscription. |

### Item holding & Staff handling — `FR-HOLD`
| ID | Description |
|---|---|
| FR-HOLD-01 | `SelfHeld`: the post goes to `Open` immediately; the finder is the holder. |
| FR-HOLD-02 | `Custodial`: `PendingDropoff` → Staff confirms receipt of the item (records the location) → `Open`. |
| FR-HOLD-03 | Staff manages stored items: list, filter by status/type, edit the storage location. |

### Claims (return requests) — `FR-CLAIM`
| ID | Description |
|---|---|
| FR-CLAIM-01 | A Member submits a claim with verification details + evidence photos (optional). |
| FR-CLAIM-02 | An item with a `Pending` claim is **locked** — do not accept hastily. |
| FR-CLAIM-03 | The holder accepts one claim → `Accepted`; the other claims on the same item are automatically `Rejected`. |
| FR-CLAIM-04 | The holder rejects a claim with a `RejectReason`. |
| FR-CLAIM-05 | **Two-way** confirmed handover: the holder confirms "handed over" + the claimant confirms "received" → `Returned`. |
| FR-CLAIM-06 | *(nice to have)* Dispute: multiple claims on the same item → staff compares the evidence, picks the winner, and writes a log entry. |

### Public event timeline — `FR-TL`
| ID | Description |
|---|---|
| FR-TL-01 | The post detail shows a timeline built from `AuditLog`, filtered by `EntityId` + `IsPublic = true`. |
| FR-TL-02 | Do not expose PrivateMarks / verification content / evidence photos on the timeline. |
| FR-TL-03 | On `Returned`: finalize the timeline + display the thank-you message (if any). |

### Notification & realtime — `FR-NOTI`
| ID | Description |
|---|---|
| FR-NOTI-01 | Each business event → write one `Notification` record. |
| FR-NOTI-02 | Push realtime over a SignalR Hub to the online user/group. |
| FR-NOTI-03 | Bell UI: unread count, list, mark-as-read, link to the target object. |
| FR-NOTI-04 | Group by `userId` (personal) + `"staff"` (shared dashboard). |

### Camera-check requests — `FR-CAM`
| ID | Description |
|---|---|
| FR-CAM-01 | A Member creates a request: area + time range + item description. |
| FR-CAM-02 | Sent to the `"staff"` group (realtime notify). State: `Pending → InReview → Resolved/Rejected`. |
| FR-CAM-03 | Staff responds (found / not found / more information needed) → notify the requester. |
| FR-CAM-04 | (Just a channel for submitting requests + responses; does NOT integrate with a real camera — out of scope.) |

### Thanking the finder — `FR-THANK`
| ID | Description |
|---|---|
| FR-THANK-01 | After `Returned`, the recipient sends a `ThankYou` (Rating 1–5 + a thank-you note) to the finder. |
| FR-THANK-02 | Display the thank-you on the post detail + the finder's profile. Notify the finder. |
| FR-THANK-03 | *(optional)* Tally reputation / badges / a finder leaderboard. |

### Admin — `FR-ADMIN`
| ID | Description |
|---|---|
| FR-ADMIN-01 | CRUD for **2-level** Category (ParentId). Users only select; they cannot create their own. |
| FR-ADMIN-02 | CRUD for Location. |
| FR-ADMIN-03 | Tag management (view, merge, clean up junk tags). |
| FR-ADMIN-04 | Handle overdue unclaimed items: `Unclaimed` → `Disposed`. |
| FR-ADMIN-05 | Dashboard: number of found-item posts, successful-return rate, average return time, longest-held items, top finders. |
| FR-ADMIN-06 | A page to view `AuditLog` (filter by actor / entity / time). |

### Audit log — `FR-LOG`
| ID | Description |
|---|---|
| FR-LOG-01 | Write an `AuditLog` for every status change + important action (set `IsPublic` correctly). |
| FR-LOG-02 | Log writing happens **within the same transaction** as the business action. |

---

## 3. Non-Functional Requirements

| ID | Description |
|---|---|
| NFR-01 | Two-tier validation: Data Annotation/Service (code) + Fluent API (real DB constraint). |
| NFR-02 | Responsive Bootstrap 5 UI; forms use TagHelper; reuse PartialView. |
| NFR-03 | Thin controllers, logic in the Service. Do not pass an Entity to the View (use a ViewModel). |
| NFR-04 | Status changes comply with the state machine; do not move to `Returned` when one confirmation is missing. |
| NFR-05 | `FoundAt` is not in the future; `StudentOrStaffCode` & `Tag.NormalizedTag` are UNIQUE; `Rating` is 1–5. |
| NFR-06 | Do not expose hidden fields (PrivateMarks, verification content) in any public location. |

---

## 4. 2-Developer work split

> Week 1: **shared work** on the foundation. From week 2, branch off per the table below.

### 👤 Dev A — "Items · Tags · Matching · Item holding"
**Owns:** `FR-FOUND-*`, `FR-TAG-*`, `FR-MATCH-*`, `FR-HOLD-*`, `FR-LOG-*`
- Entities & configuration: `FoundItem`, `Category` (2 levels), `Location`, `Tag`/`FoundItemTag`, `LostAlert`.
- Services: `FoundItemService`, `TagService` (owns the normalization function), `MatchingService`, `AuditService` (owns the impl).
- Views: the found-item report form (public + PrivateMarks + tag input), public list + detail, search/filter/pagination, the Staff stored-items screen (FR-HOLD-02/03).
- **Finalize the `FoundItem` state machine** + the **`TagService.Normalize` function** (both shared across the two devs).

### 👤 Dev B — "Accounts · Claims · Notification · Timeline · Auxiliary modules · Admin"
**Owns:** `FR-AUTH-*`, `FR-CLAIM-*`, `FR-NOTI-*`, `FR-TL-*`, `FR-CAM-*`, `FR-THANK-*`, `FR-ADMIN-*`
- Identity: configuration, seed roles + sample accounts, the user-management page.
- Entities & configuration: `Claim`, `Notification`, `AuditLog` (schema + `IsPublic`), `CameraCheckRequest`, `ThankYou`.
- Services: `ClaimService`, `NotificationService` (owns the impl), `CameraRequestService`, `ThankYouService`, `DashboardService`.
- SignalR Hub + client JS + the bell UI (`_NotificationBell`) + the `_Timeline` PartialView.
- Views: the claim form, the accept/reject/two-way-handover screens, the public timeline, camera request, thank-you, the entire Admin area + dashboard + log viewer.
- **Finalize the `Claim` state machine.**

### Workload balance
- A is heavy on **matching + tag normalization** + many data-entry/lookup CRUD screens.
- B is heavy on **SignalR realtime** + the **claim/two-way-handover/dispute state machine** + many auxiliary modules + Admin.
→ Reasonably even. The "hard-to-score" work is split one area each (matching for A, SignalR for B).

---

## 5. Shared contracts (finalize early so neither side is blocked)

Agree **right in week 1** and code to the interfaces:

1. **Entity + DbContext** — built together in week 1; whoever changes their own entity notifies the other side.
2. **Status enums (locked hard, no renaming midway):**
   - `FoundItemStatus { PendingDropoff, Open, ClaimAccepted, Returned, Unclaimed, Disposed }`
   - `ClaimStatus { Pending, Accepted, Rejected }`
   - `HoldingType { SelfHeld, Custodial }`
   - `CameraRequestStatus { Pending, InReview, Resolved, Rejected }`
3. **`ITagService`** (A owns the impl, B calls it when subscribing/comparing):
   ```csharp
   string Normalize(string raw);
   Task<IEnumerable<Tag>> ResolveTagsAsync(IEnumerable<string> rawTags); // create if not present
   ```
4. **`INotificationService`** (B owns the impl, A calls it during matching):
   ```csharp
   Task PushAsync(string recipientUserId, string type, string title,
                  string message, string linkUrl);
   Task PushToStaffAsync(string type, string title, string message, string linkUrl);
   ```
5. **`IAuditService`** (A owns the impl, both call it):
   ```csharp
   Task LogAsync(string actorUserId, string action, string entityType, string entityId,
                 string? fromStatus, string? toStatus, string? detail, bool isPublic);
   ```
6. **Shared PartialViews:** `_ItemCard`, `_Pager`, `_TagInput` (A builds them); `_NotificationBell`, `_Timeline`, `_ClaimForm` (B builds them).

> Git: each person works on a `feature/<name>` branch, merging into `dev` via Pull Request (quick cross-review). `main` holds only runnable builds.

---

## 6. Per-person timeline

| Week | Dev A | Dev B |
|---|---|---|
| 1 | (shared) entities + DbContext + 2-level Category + layout + finalize the FoundItem state machine + the Normalize function | (shared) Identity + seed roles/accounts + finalize the Claim state machine + enums |
| 2 | FR-FOUND + FR-TAG (report found item, public list/detail, search/filter/pagination, tag input) | FR-AUTH-05 (user management) + build the `Notification`/`AuditLog`/`Claim` schema |
| 3 | FR-HOLD (SelfHeld vs Custodial, Staff receiving stored items) + FR-LOG impl | FR-CLAIM-01/02 (submit claim, lock item) + SignalR Hub skeleton + `_Timeline` |
| 4 ⭐ | FR-MATCH (LostAlert + publish/subscribe) → calls `INotificationService` | FR-CLAIM-03/04/05 (accept/reject/two-way handover) + FR-NOTI + FR-TL |
| 5 | Support FR-CLAIM-06 (dispute) + fill in validation/test the item flow + FR-TAG-04 (autocomplete) | FR-CAM + FR-THANK + FR-ADMIN (Category/Location/Tag, unclaimed items, dashboard, log viewer) |
| 6 | (shared) UI polish · test happy path + error cases · write the report/slides | (shared) same as A |

---

## 7. Definition of Done (per FR)

- [ ] Full two-tier validation (code + DB).
- [ ] Correct authorization, tested with an under-privileged account.
- [ ] An AuditLog is written if it is an important action (set `IsPublic` correctly).
- [ ] Status changes follow the state machine; do not move to `Returned` when a confirmation is missing.
- [ ] Tag/subscribe comparison runs on the normalized form (the same single function).
- [ ] Do not expose hidden fields in any public location.
- [ ] UI uses TagHelper + PartialView, responsive.
- [ ] A notification is sent if the event involves another user.
- [ ] Test both the happy path and error cases (claim in the wrong state, locked item, dispute, etc.).
