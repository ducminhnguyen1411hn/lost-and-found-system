using LostAndFound.Data;
using LostAndFound.Models.Enums;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

/// <inheritdoc />
public class UnclaimedSweepService : IUnclaimedSweepService
{
    // Days an item may stay Open before it's considered unclaimed. Lower this (e.g. 0) to demo the sweep
    // on fresh data, where every item's CreatedAt is "now".
    private const int Threshold = 30;

    private readonly ApplicationDbContext _db;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public UnclaimedSweepService(ApplicationDbContext db, IAuditService auditService, INotificationService notificationService)
    {
        _db = db;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public int OverdueDays => Threshold;

    public async Task<int> SweepOverdueAsync(string? actorUserId = null)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Threshold);
        var openStatus = (int)FoundItemStatus.Open;
        var pendingClaim = (int)ClaimStatus.Pending;

        // Only Open items past the cutoff that NO ONE is actively claiming (no pending claim).
        var candidates = await _db.FoundItem
            .Where(f => f.Status == openStatus
                     && f.CreatedAt < cutoff
                     && !f.Claim.Any(c => c.Status == pendingClaim))
            .ToListAsync();

        if (candidates.Count == 0) return 0;

        await using var tx = await _db.Database.BeginTransactionAsync();

        foreach (var item in candidates)
            item.Status = (int)FoundItemStatus.Unclaimed;
        await _db.SaveChangesAsync();

        foreach (var item in candidates)
        {
            // Internal lifecycle event (pulled from circulation for disposal): keep it off the public timeline.
            await _auditService.LogAsync(actorUserId ?? string.Empty, "MarkedUnclaimed", "FoundItem",
                item.Id.ToString(), FoundItemStatus.Open.ToString(), FoundItemStatus.Unclaimed.ToString(),
                $"Quá {Threshold} ngày không có người nhận — đánh dấu chưa có người nhận.", isPublic: false);

            await _notificationService.PushAsync(item.ReporterUserId, "ItemUnclaimed",
                "Đồ của bạn quá hạn chưa có người nhận",
                $"Đồ \"{item.Title}\" đã quá {Threshold} ngày không ai nhận và được đánh dấu chưa có người nhận.",
                $"/FoundItems/Details/{item.Id}");
        }

        await tx.CommitAsync();
        return candidates.Count;
    }
}
