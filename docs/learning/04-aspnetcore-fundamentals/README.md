# Module 04 — ASP.NET Core fundamentals (Program.cs, DI, middleware)

> **Why this matters here:** `Program.cs` is where every service is registered and the request
> pipeline is wired. When you build `TagService`, `NotificationService`, or a SignalR hub, you'll
> register them here. Understanding DI is the single biggest "aha" for ASP.NET Core.

**Time:** ~1.5h · **Prerequisites:** 01 · **Fast track:** ⭐

---

## 1. Read the real `Program.cs`

Open [`Program.cs`](../../../LostAndFound/Program.cs). It has exactly two halves:

```csharp
var builder = WebApplication.CreateBuilder(args);

// ── HALF 1: register SERVICES (the DI container) ──────────────────
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddDefaultIdentity<ApplicationUser>(...).AddRoles<IdentityRole>()
       .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();   // MVC
builder.Services.AddRazorPages();             // default Identity UI
builder.Services.AddSignalR();
// builder.Services.AddScoped<ITagService, TagService>();   // ← you'll uncomment these later

var app = builder.Build();

// ── HALF 2: build the MIDDLEWARE PIPELINE (order matters!) ─────────
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();   // who are you?
app.UseAuthorization();    // are you allowed?
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
await app.RunAsync();
```

**Half 1** says *what objects the app can create*. **Half 2** says *what steps each request goes
through*. Everything you add lands in one of these two halves.

📖 [App startup / Program.cs](https://learn.microsoft.com/aspnet/core/fundamentals/?view=aspnetcore-8.0)

---

## 2. Dependency Injection (DI) — the core idea

A class **declares what it needs** in its constructor; the framework **creates and supplies** it. You
never write `new TagService()` in a controller. Instead:

```csharp
// 1. Register the mapping (in Program.cs):  "when someone asks for ITagService, give them TagService"
builder.Services.AddScoped<ITagService, TagService>();

// 2. Ask for it (in a controller/service constructor):
public class FoundItemsController(ITagService tags) : Controller { /* use `tags` */ }
```

Why this is worth it:
- **Swappable** — depend on `ITagService`, not the concrete class; change implementations freely.
- **Testable** — pass a fake in tests.
- **Lifetimes managed for you** — the container creates and disposes objects at the right time.

This is why the interfaces in [`Services/Interfaces/`](../../../LostAndFound/Services/Interfaces/)
exist *before* their implementations: the contract is agreed first, then registered, then injected.

📖 [Dependency injection in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-8.0)

---

## 3. Service lifetimes (pick the right one)

When you register a service you choose how long an instance lives:

| Method | Lifetime | Use for |
|---|---|---|
| `AddScoped<T>()` | **one instance per HTTP request** | **almost everything**: your services, anything that uses `DbContext` |
| `AddTransient<T>()` | new instance every time it's asked for | cheap, stateless helpers |
| `AddSingleton<T>()` | one instance for the whole app | caches, config; **never** something holding a `DbContext` |

**The rule that bites beginners:** `DbContext` is registered **Scoped** (that's what `AddDbContext`
does). So any service that uses it must be **Scoped** too. Don't inject a Scoped service into a
Singleton — it captures one request's `DbContext` forever and corrupts state.

For this project: register your `TagService`, `AuditService`, `NotificationService` with
**`AddScoped`**.

📖 [Service lifetimes](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/service-lifetimes)

---

## 4. Middleware & pipeline order

Each `app.Use...()` is a step a request passes through, **in the order written**. The response then
flows back up. Order is not cosmetic — get it wrong and auth silently breaks. The fixed order for an
MVC app:

```
UseHttpsRedirection → UseStaticFiles → UseRouting → UseAuthentication → UseAuthorization → endpoints
```

- `UseStaticFiles` early so CSS/JS/images return without hitting controllers.
- `UseRouting` figures out which endpoint matches **before** auth runs.
- **`UseAuthentication` must come before `UseAuthorization`** — you can't check "are you allowed?"
  before "who are you?". The real file even comments this.
- `MapControllerRoute` / `MapRazorPages` are the **endpoints** — the end of the pipe where your action
  actually runs.

📖 [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0)

---

## 5. Configuration (connection strings & settings)

Settings come from `appsettings.json` (+ environment-specific overrides + environment variables). The
DB connection string is read like this in `Program.cs`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
```

`appsettings.json` has a `"ConnectionStrings": { "DefaultConnection": "...localdb..." }` section.
**Never** hard-code secrets in C#; read them from configuration.

📖 [Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0)

---

## 🛠️ Exercise

You won't change behavior — you'll *read and predict*. In `Program.cs`:

1. Find the three commented-out `AddScoped` lines. In one sentence each, say what registering
   `ITagService`, `IAuditService`, `INotificationService` will let you do later.
2. The comment says "must come before `UseAuthorization`". Predict what would break if you swapped
   `UseAuthentication` and `UseAuthorization`. (Answer: authorization runs before the user identity
   is established, so `[Authorize]` would reject logged-in users.)
3. Write (on paper) the exact line you'd add to register a Scoped `TagService` for `ITagService`.
4. Explain why `TagService` must be `AddScoped` and not `AddSingleton`. (Hint: it'll use `DbContext`.)

---

## ✅ Self-check

- [ ] I can point to the two halves of `Program.cs` (register services vs. build pipeline).
- [ ] I can explain DI in one sentence and write an `AddScoped<IFoo, Foo>()` registration.
- [ ] I know the three lifetimes and why DB-touching services must be Scoped.
- [ ] I know why `UseAuthentication` precedes `UseAuthorization`.
- [ ] I know where the DB connection string comes from.

---

## 📚 Microsoft Learn (.NET 8)

- [Dependency injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-8.0)
- [Middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0)
- [DI into controllers](https://learn.microsoft.com/aspnet/core/mvc/controllers/dependency-injection?view=aspnetcore-8.0)
- [Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0)

➡️ Next: [Module 05 — MVC & routing](../05-mvc-and-routing/)
