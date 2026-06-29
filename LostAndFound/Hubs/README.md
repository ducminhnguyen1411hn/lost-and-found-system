# Hubs

SignalR hubs go here. `AddSignalR()` is already wired in `Program.cs`. When you add the first hub:
create the hub class here, then `app.MapHub<YourHub>("/hubs/your-hub")` in `Program.cs`.

Groups convention (from the spec): per-user group = `userId`, plus a shared `"staff"` group.
Empty for now — the first hub is feature work (FR-NOTI).
