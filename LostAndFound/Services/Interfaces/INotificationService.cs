namespace LostAndFound.Services.Interfaces;

/// <summary>
/// Shared contract (REQUIREMENTS §5). Dev B owns the implementation (DB-backed + SignalR push);
/// Dev A calls it from matching. Each call writes a Notification row AND pushes realtime.
/// No DI registration yet — wire it when the implementation lands.
/// </summary>
public interface INotificationService
{
    /// <summary>Notify a single user (personal SignalR group = userId).</summary>
    Task PushAsync(string recipientUserId, string type, string title, string message, string linkUrl);

    /// <summary>Notify the whole staff group (SignalR group "staff").</summary>
    Task PushToStaffAsync(string type, string title, string message, string linkUrl);
}
