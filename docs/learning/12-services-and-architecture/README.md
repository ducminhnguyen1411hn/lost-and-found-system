# Module 12 — Services & architecture

> **Why this matters here:** this is *the* architectural rule of the repo. Business logic lives in a
> **Service**, and a service does the rule **+ writes `AuditLog` + pushes `Notification`** in **one
> transaction**. Master this module and your features will pass the Definition of Done.

**Time:** ~2h · **Prerequisites:** 06, 07, 10, 11 · **Fast track:** (do after the ⭐ track)

---

## 1. The layering, top to bottom

```
Controller (thin)         reads input, checks ModelState + ownership, calls a service, picks a result
   │  calls
   ▼
Service (the brain)       enforces the business rule + state machine,
                          writes the AuditLog row, queues the Notification — all in ONE transaction
   │  uses
   ▼
DbContext / entities      EF Core persistence
```

Why split it out:
- **Testable** — the rule can be unit-tested without HTTP.
- **Reusable** — a controller, a background job, and a SignalR hub can all call the same service.
- **Atomic** — putting the rule + audit + notify in one place lets them share one transaction so they
  can't drift apart.

📖 [Architectural principles](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles) ·
[Common web app architectures](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

---

## 2. Contract first, then implementation, then DI

The repo already defines contracts in [`Services/Interfaces/`](../../../LostAndFound/Services/Interfaces/):
`ITagService` (Dev A), `IAuditService` (Dev A), `INotificationService` (Dev B). They're agreed shapes
so two devs don't block each other. The flow to add a service:

1. **Interface** exists (or you add one, after agreeing the signature).
2. **Implement** it: `public class FoundItemService(ApplicationDbContext db, IAuditService audit, INotificationService noti) : IFoundItemService`.
3. **Register** it in `Program.cs` (uncomment / add): `builder.Services.AddScoped<IFoundItemService, FoundItemService>();`
   (Scoped — it uses `DbContext`; Module 04.)
4. **Inject** the interface into the controller.

The three feature interfaces are intentionally **not DI-registered yet** — wire them when their
implementation lands.

---

## 3. The signature pattern: rule + audit + notify in one transaction

Here is the shape every state-changing service method follows. Read it slowly — it encodes four DoD
items at once (legal transition, audit with right `IsPublic`, notification, atomicity):

```csharp
public async Task AcceptClaimAsync(int claimId, string actingUserId)
{
    var claim = await db.Claim.Include(c => c.FoundItem)
                              .FirstOrDefaultAsync(c => c.Id == claimId)
                ?? throw new NotFoundException();

    var item = claim.FoundItem;

    // 1) STATE-MACHINE GUARD — is this transition even legal?
    if ((FoundItemStatus)item.Status != FoundItemStatus.Open)
        throw new InvalidOperationException("Item must be Open to accept a claim.");

    // 2) THE RULE — accept this claim, auto-reject the others on the item
    claim.Status = (int)ClaimStatus.Accepted;
    item.Status  = (int)FoundItemStatus.ClaimAccepted;
    var others = await db.Claim
        .Where(c => c.FoundItemId == item.Id && c.Id != claimId && c.Status == (int)ClaimStatus.Pending)
        .ToListAsync();
    foreach (var o in others) o.Status = (int)ClaimStatus.Rejected;

    // 3) AUDIT — one row per status change, with the correct visibility
    db.AuditLog.Add(new AuditLog {
        EntityType = "FoundItem", EntityId = item.Id,
        Action = "ClaimAccepted", ActorUserId = actingUserId,
        IsPublic = true,                       // a public timeline event (no secrets in the message)
        CreatedAt = DateTime.UtcNow
    });

    // 4) NOTIFY — the other party is involved, so tell them
    await noti.QueueAsync(claim.ClaimantUserId, $"Your claim on \"{item.Title}\" was accepted.");

    // 5) ONE TRANSACTION — all of the above commit together, or none do
    await db.SaveChangesAsync();
}
```

Key points:
- **One `SaveChangesAsync()`** at the end makes steps 2–4 a single atomic transaction (Module 10 §3).
  If the notify insert fails, the accept is rolled back too — they can never disagree.
- **`IsPublic`** must be set deliberately: public timeline events vs. internal-only audit. Never put
  `PrivateMarks` or verification details into a public audit message (blind-listing).
- The controller calling this stays tiny: ownership check (Module 11) → `await service.AcceptClaimAsync(...)`
  → redirect.

> If you need work across **multiple** `SaveChanges` calls (e.g. matching that fans out many
> notifications), wrap them in an explicit `await using var tx = await db.Database.BeginTransactionAsync();`
> … `await tx.CommitAsync();`.

📖 [Transactions](https://learn.microsoft.com/ef/core/saving/transactions) ·
[Saving data](https://learn.microsoft.com/ef/core/saving/)

---

## 4. The state machine is the service's job

The legal transitions for `FoundItem` (full diagram in [`../../INDEX.md`](../../INDEX.md) §5):

```
SelfHeld  → Open                         Open → ClaimAccepted   (holder accepts a claim)
Custodial → PendingDropoff → Open        ClaimAccepted → Returned   (BOTH handovers confirmed)
                                         ClaimAccepted → Open       (holder cancels)
Open → Unclaimed → Disposed              (time-based / admin)
```

Invariants the service must protect:
- At most **one** `Accepted` claim per item; accepting one **auto-rejects** the others (shown above).
- A `Pending` claim **locks** the item (block conflicting transitions).
- `Returned` only when **both** `HolderConfirmedHandover` **and** `ClaimantConfirmedHandover` are true.
- **Every** status change writes exactly one `AuditLog` row.

Guard each transition with an explicit check (step 1 above). Illegal transition → throw / return an
error; never silently allow it.

---

## 5. The single tag normalizer (a shared-service example)

`ITagService.Normalize` is the **one and only** tag normalizer: trim + lower + strip Vietnamese
diacritics + collapse whitespace. The rule: **display** uses the raw tag, but **matching / subscribe
always compare on `NormalizedTag`.** Every place that touches tags calls this one method — never
reimplement normalization inline, or matching will quietly break.

```csharp
// matching a found item's tags against a LostAlert subscription:
var norm = tags.Normalize(userInput);
bool matches = await db.LostAlertTag.AnyAsync(t => t.NormalizedTag == norm);
```

This is also why `Tag.NormalizedTag` is `UNIQUE` in the DB (Module 07 tier-2): the normalizer dedupes
in code, the constraint guarantees it in storage.

---

## 🛠️ Exercise

Promote your `FR-FOUND` logic from the controller into a service:

1. Define `IFoundItemService` with `Task<int> CreateAsync(CreateFoundItemVm vm, string reporterUserId)`.
2. Implement `FoundItemService` (primary constructor injecting `ApplicationDbContext` + `IAuditService`):
   build the entity (Status = Open or PendingDropoff depending on Custodial), `Add` it, **add one
   `AuditLog` row** ("Created", set `IsPublic` thoughtfully), then a single `SaveChangesAsync`; return
   the new id.
3. Register it in `Program.cs` with `AddScoped`. Make the controller's `Create` POST just call it.
4. **Design (write, don't necessarily run) `AcceptClaimAsync`** following section 3: the state guard,
   the auto-reject of sibling claims, the audit row, the notification, the single save. List which DoD
   items each numbered step satisfies.

---

## ✅ Self-check

- [ ] I can explain why business logic goes in a service, not the controller.
- [ ] I can write a service method that does rule + audit + notify in **one** `SaveChangesAsync`.
- [ ] I set `AuditLog.IsPublic` deliberately and never leak hidden fields into it.
- [ ] I guard each state transition and know the FoundItem invariants (one Accepted, Pending locks, both-confirm-to-Return).
- [ ] I register services as `AddScoped` and inject the interface.
- [ ] I always normalize tags through `ITagService.Normalize` and match on `NormalizedTag`.

---

## 📚 Microsoft Learn (.NET 8)

- [Architectural principles](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [Common web app architectures](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [EF Core transactions](https://learn.microsoft.com/ef/core/saving/transactions)
- [DI in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-8.0)

➡️ Next: [Module 13 — SignalR (realtime)](../13-signalr-realtime/)
