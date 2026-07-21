using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Services.Interfaces;

namespace LostAndFound.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(string actorUserId, string action, string entityType, string entityId,
                           string? fromStatus, string? toStatus, string? detail, bool isPublic)
    {
        _db.AuditLog.Add(new AuditLog
        {
            ActorUserId = string.IsNullOrEmpty(actorUserId) ? null : actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Detail = detail,
            IsPublic = isPublic

        });
        await _db.SaveChangesAsync();
    }
}
