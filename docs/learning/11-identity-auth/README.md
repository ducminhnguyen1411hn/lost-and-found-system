# Module 11 — Identity, authentication & ownership

> **Why this matters here:** every feature has an authorization requirement. The Definition of Done
> demands the correct `[Authorize]` **and** an ownership check. This app's twist: claim-approval rights
> follow **who holds the item**, not the role — so role checks alone are not enough.

**Time:** ~1.5h · **Prerequisites:** 04, 06 · **Fast track:** ⭐

---

## 1. Authentication vs authorization

- **Authentication** = *who are you?* (logging in). Handled by ASP.NET Core Identity + the default
  Identity UI (`/Identity/Account/Login`, `/Register`). Already wired in `Program.cs` via
  `AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>()`.
- **Authorization** = *are you allowed to do this?* (the `[Authorize]` attribute + your own checks).

The four roles (seeded in `SeedData.cs`): `Guest` (anonymous), `Member`, `Staff`, `Admin`. See the
role table in [`../../INDEX.md`](../../INDEX.md) §3.

📖 [Introduction to Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity?view=aspnetcore-8.0)

---

## 2. `[Authorize]` — the first gate

Attributes on a controller or action decide access before the action runs:

```csharp
[Authorize]                                  // any signed-in user
public class FoundItemsController : Controller
{
    [AllowAnonymous]                          // exception: this one is public (Guest can browse)
    public async Task<IActionResult> Index() { ... }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id) { ... }

    // Create requires login (inherits the controller's [Authorize]) — Members report items
    public IActionResult Create() => View();
}

[Authorize(Roles = "Admin")]                  // only Admins
public class AdminController : Controller { ... }

[Authorize(Roles = "Staff,Admin")]            // Staff OR Admin
public class CustodyController : Controller { ... }
```

- No `Roles` → just "must be logged in."
- `Roles = "Staff,Admin"` → in **any** of those roles.
- `[AllowAnonymous]` opens a specific action on an otherwise-protected controller (Guest browsing).

📖 [Simple authorization](https://learn.microsoft.com/aspnet/core/security/authorization/simple?view=aspnetcore-8.0) ·
[Role-based authorization](https://learn.microsoft.com/aspnet/core/security/authorization/roles?view=aspnetcore-8.0)

---

## 3. The ownership check — the second gate (don't skip it!)

`[Authorize]` answers "is this *a* Member?" It does **not** answer "is this *the right* Member for
*this* item?" That second question is an **ownership check** you write yourself, after loading the
entity:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AcceptClaim(int claimId)
{
    var claim = await db.Claim.Include(c => c.FoundItem).FirstOrDefaultAsync(c => c.Id == claimId);
    if (claim is null) return NotFound();

    var myId = userManager.GetUserId(User);

    // Rule: only the HOLDER of the item may accept a claim on it.
    if (!IsHolder(claim.FoundItem, myId)) return Forbid();   // 403, not 404

    // ... hand off to the service to do the state change + audit + notify
}
```

Without that check, any logged-in Member could accept claims on *other people's* items just by
guessing an id. `[Authorize]` alone would let them through. **Authorize = the door; ownership = the
key to this specific room.**

📖 [Resource-based authorization](https://learn.microsoft.com/aspnet/core/security/authorization/resourcebased?view=aspnetcore-8.0)

---

## 4. "Holder", not role — the project-specific rule

This is the subtle bit unique to LostAndFound. Who may approve/reject a claim depends on **how the
item is held** (`HoldingType`), not on a fixed role:

- `HoldingType.SelfHeld` → the **reporter** is the holder (`item.ReporterUserId`).
- `HoldingType.Custodial` → **staff** is the holder (`item.CustodianStaffId`).

So derive the holder from the data; never hardcode "only Staff can approve":

```csharp
private static bool IsHolder(FoundItem item, string userId) =>
    (HoldingType)item.HoldingType == HoldingType.SelfHeld
        ? item.ReporterUserId == userId
        : item.CustodianStaffId == userId;
```

(Admins/Staff may also act in arbitration — model that explicitly, e.g. `IsHolder(...) || User.IsInRole("Staff")`,
according to the requirement you're implementing.)

---

## 5. Getting the current user

```csharp
// inject UserManager<ApplicationUser> via the constructor
string? myId = userManager.GetUserId(User);     // the AspNetUsers Id (a string)
bool isStaff = User.IsInRole("Staff");
bool signedIn = User.Identity?.IsAuthenticated == true;
```

Remember from Module 10: user FKs (`ReporterUserId`, `CustodianStaffId`) are plain strings, so you
compare ids directly — no navigation property.

### Show/hide UI by role (cosmetic only)
In a view you can inject the user to hide buttons, but **never** rely on hiding for security — the
server check is what protects the action:

```cshtml
@if (User.IsInRole("Staff")) { <a asp-action="Intake" class="btn btn-warning">Staff intake</a> }
```

---

## 🛠️ Exercise

Add authorization to your `FoundItemsController` from earlier modules:

1. Put `[Authorize]` on the controller; mark `Index` and `Details` `[AllowAnonymous]` (Guests browse).
2. `Create` (GET+POST) should require login — confirm an anonymous request redirects to Login.
3. Write `AcceptClaim(int claimId)` POST with: load claim + item, `NotFound` if missing, the
   `IsHolder` ownership check returning `Forbid()` if it fails. (The state change itself comes in
   Module 12.)
4. Write the `IsHolder` helper (section 4) and test both branches mentally: a SelfHeld item's reporter
   vs. a random Member; a Custodial item's custodian vs. its original reporter.
5. In the Details view, show an "Accept claim" button **only** to the holder — but keep the server
   `Forbid()` check anyway. Explain why both are needed.

---

## ✅ Self-check

- [ ] I can distinguish authentication from authorization.
- [ ] I can apply `[Authorize]`, `[Authorize(Roles=...)]`, and `[AllowAnonymous]` correctly.
- [ ] I know `[Authorize]` is not enough — I always add an ownership check for per-record actions.
- [ ] I can derive the "holder" from `HoldingType` instead of hardcoding a role.
- [ ] I can get the current user's id/roles and compare against string FKs.
- [ ] I know hiding a button in the view is cosmetic; the server check is the real protection.

---

## 📚 Microsoft Learn (.NET 8)

- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity?view=aspnetcore-8.0)
- [Simple authorization](https://learn.microsoft.com/aspnet/core/security/authorization/simple?view=aspnetcore-8.0)
- [Role-based authorization](https://learn.microsoft.com/aspnet/core/security/authorization/roles?view=aspnetcore-8.0)
- [Resource-based authorization](https://learn.microsoft.com/aspnet/core/security/authorization/resourcebased?view=aspnetcore-8.0)

➡️ Next: [Module 12 — Services & architecture](../12-services-and-architecture/)
