namespace LostAndFound.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(string actorUserId, string action, string entityType, string entityId,
                  string? fromStatus, string? toStatus, string? detail, bool isPublic);
}
