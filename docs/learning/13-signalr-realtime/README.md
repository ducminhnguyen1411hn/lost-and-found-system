# Module 13 — SignalR (realtime notifications)

> **Why this matters here:** realtime notification is one of the three core values of this product.
> When a match is found or a claim is accepted, the affected user should see it **without refreshing**.
> SignalR is the tool. `AddSignalR()` is already in `Program.cs`; you'll add the hub.

**Time:** ~1.5h · **Prerequisites:** 04, 12 · **Fast track:** (do after the ⭐ track)

---

## 1. What SignalR gives you

A normal web request is one-shot: browser asks, server answers, done. SignalR keeps a **persistent
connection** open so the **server can push** to the browser at any time. A **Hub** is the server class
that defines what can be called; the browser uses the SignalR JS client to connect and listen.

```
Service (e.g. accept claim) ──► IHubContext.Clients.User(id).SendAsync("ReceiveNotification", text)
                                        │  (server push over the open connection)
                                        ▼
Browser JS:  connection.on("ReceiveNotification", text => showToast(text))
```

📖 [Introduction to SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction?view=aspnetcore-8.0)

---

## 2. The hub (server side)

A hub is a class inheriting `Hub`, in the `Hubs/` folder:

```csharp
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace LostAndFound.Hubs;

[Authorize]                                  // only signed-in users connect
public class NotificationHub : Hub { }       // no methods needed; the server only PUSHES here
```

Map it in `Program.cs` (the file already has a placeholder comment for this):

```csharp
app.MapHub<NotificationHub>("/hubs/notifications");
```

📖 [SignalR hubs](https://learn.microsoft.com/aspnet/core/signalr/hubs?view=aspnetcore-8.0)

---

## 3. Pushing from a service (the important part)

You don't push from the hub class itself — you inject **`IHubContext<NotificationHub>`** into your
notification service and call clients from there. This is how `INotificationService` (Dev B) becomes
"realtime":

```csharp
public class NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
    : INotificationService
{
    public async Task QueueAsync(string userId, string message)
    {
        // 1) persist it (so it survives reload / offline users) — part of the DB transaction
        db.Notification.Add(new Notification {
            RecipientUserId = userId, Message = message,
            IsRead = false, CreatedAt = DateTime.UtcNow
        });
        // NOTE: the SaveChangesAsync is owned by the calling service so it's one transaction (Module 12).

        // 2) push live to that user's open connections (best-effort; they may be offline)
        await hub.Clients.User(userId).SendAsync("ReceiveNotification", message);
    }
}
```

`Clients.User(userId)` targets a specific signed-in user across all their tabs — exactly right for
"tell the claimant their claim was accepted." (It works because Identity gives SignalR the user id.)

> Order matters: persist first (so the notification isn't lost if the user is offline), then push.
> The live push is a bonus on top of the stored record.

📖 [Send from outside a hub (`IHubContext`)](https://learn.microsoft.com/aspnet/core/signalr/hubcontext?view=aspnetcore-8.0) ·
[Users and groups](https://learn.microsoft.com/aspnet/core/signalr/groups?view=aspnetcore-8.0)

---

## 4. The browser client

Add the SignalR JS client (LibMan, into `wwwroot/js/signalr/`) and a small script, typically wired in
`_Layout.cshtml` so the bell updates on every page:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications")
    .withAutomaticReconnect()
    .build();

// register handlers BEFORE start()
connection.on("ReceiveNotification", (message) => {
    // e.g. bump the bell badge + show a Bootstrap toast
    showToast(message);
});

connection.start().catch(err => console.error(err));
```

The method name `"ReceiveNotification"` must match exactly between the server `SendAsync(...)` and the
client `connection.on(...)`.

📖 [JavaScript client](https://learn.microsoft.com/aspnet/core/signalr/javascript-client?view=aspnetcore-8.0) ·
[Get started tutorial](https://learn.microsoft.com/aspnet/core/tutorials/signalr?view=aspnetcore-8.0)

---

## 5. Where it fits the workflow

The realtime push is the *last* step of a service action, after the DB work:

```
AcceptClaimAsync:  state change → AuditLog → noti.QueueAsync(claimant, "...accepted")
                                                     │
                                                     ├─ insert Notification row  (in the transaction)
                                                     └─ hub push "ReceiveNotification"  (live)
```

Matching (`FR-MATCH`) is the other big trigger: when a new found item's normalized tags match a
`LostAlert` subscription, queue a notification to that subscriber — which both stores and live-pushes.

---

## 🛠️ Exercise

Wire a minimal realtime path end-to-end:

1. Create `Hubs/NotificationHub.cs` (an `[Authorize]` empty `Hub`) and map it in `Program.cs` at
   `/hubs/notifications`.
2. Sketch `NotificationService.QueueAsync` (section 3): inject `IHubContext<NotificationHub>`, add a
   `Notification` row, push `"ReceiveNotification"` to `Clients.User(userId)`. Register it
   `AddScoped<INotificationService, NotificationService>()`.
3. Add the SignalR JS client + the connect/`on`/`start` script to `_Layout.cshtml`; show an alert/toast
   on receive.
4. Test: open two browsers logged in as two users; have an action call `QueueAsync` for user B and
   confirm B sees it live while A triggered it. If B is offline, confirm the row is still stored.

---

## ✅ Self-check

- [ ] I can explain why SignalR exists (server → browser push over a persistent connection).
- [ ] I can create an `[Authorize]` hub and `MapHub` it.
- [ ] I know to push via `IHubContext` from a service, not from the hub class.
- [ ] I persist the notification first, then push live, and know why.
- [ ] I can target a specific user with `Clients.User(userId)` and match method names client/server.

---

## 📚 Microsoft Learn (.NET 8)

- [Introduction to SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction?view=aspnetcore-8.0)
- [Get started tutorial](https://learn.microsoft.com/aspnet/core/tutorials/signalr?view=aspnetcore-8.0)
- [Hubs](https://learn.microsoft.com/aspnet/core/signalr/hubs?view=aspnetcore-8.0) ·
  [IHubContext](https://learn.microsoft.com/aspnet/core/signalr/hubcontext?view=aspnetcore-8.0)
- [JavaScript client](https://learn.microsoft.com/aspnet/core/signalr/javascript-client?view=aspnetcore-8.0)

➡️ Next: [Module 14 — Capstone: a vertical slice](../14-capstone-vertical-slice/)
