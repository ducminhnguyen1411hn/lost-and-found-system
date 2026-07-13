using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Services.Interfaces;

namespace LostAndFound.Services;

/// <summary>
/// Writes one <see cref="AuditLog"/> row per business event (FR-LOG). Callers invoke this INSIDE the
/// same transaction as their business action so the log and the change commit atomically. This method
/// does not open its own transaction. See <see cref="IAuditService"/>.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db) => _db = db;

    /// <inheritdoc />
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
            Detail = detail,        // NEVER pass PrivateMarks / verification content here
            IsPublic = isPublic
            // CreatedAt is store-generated (DB default sysutcdatetime()).
        });
        await _db.SaveChangesAsync();
    }
}
