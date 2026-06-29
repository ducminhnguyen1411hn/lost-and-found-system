namespace LostAndFound.Services.Interfaces;

/// <summary>
/// Shared contract (REQUIREMENTS §5). Dev A owns the implementation; both devs call it inside the
/// SAME transaction as the business action. Set <paramref name="isPublic"/> carefully: public rows
/// feed the item timeline, so never expose PrivateMarks / verification content there.
/// No DI registration yet — wire it when the implementation lands.
/// </summary>
public interface IAuditService
{
    Task LogAsync(string actorUserId, string action, string entityType, string entityId,
                  string? fromStatus, string? toStatus, string? detail, bool isPublic);
}
