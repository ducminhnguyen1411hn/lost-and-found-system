using LostAndFound.Data;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Holding;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

/// <inheritdoc />
public class HoldingService : IHoldingService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public HoldingService(ApplicationDbContext db, IAuditService auditService, INotificationService notificationService)
    {
        _db = db;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<List<PendingIntakeViewModel>> GetPendingIntakeAsync()
    {
        var pending = (int)FoundItemStatus.PendingDropoff; // only Custodial items ever land here

        return await _db.FoundItem
            .Where(f => f.Status == pending)
            .OrderBy(f => f.CreatedAt)
            .Select(f => new PendingIntakeViewModel
            {
                Id = f.Id,
                Title = f.Title,
                CategoryName = f.Category.Name,
                LocationName = f.Location.Name,
                ReporterName = f.ReporterUser.FullName ?? f.ReporterUser.Email ?? "?",
                FoundAt = f.FoundAt,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> ConfirmReceiptAsync(int itemId, string storageLocation, string staffUserId)
    {
        var item = await _db.FoundItem.FirstOrDefaultAsync(f => f.Id == itemId);
        if (item is null) return false;

        // Legal transition guard: only a Custodial item still awaiting intake can be received.
        if (item.Status != (int)FoundItemStatus.PendingDropoff || item.HoldingType != (int)HoldingType.Custodial)
            return false;

        await using var tx = await _db.Database.BeginTransactionAsync();

        item.CustodianStaffId = staffUserId;   // Custodial holder is the receiving staff (never role-hardcoded)
        item.StorageLocation = storageLocation.Trim();
        item.Status = (int)FoundItemStatus.Open;
        await _db.SaveChangesAsync();

        // Public milestone ("received & opened"), but the storage location is internal — keep it OUT of the
        // public detail so it never lands on the item's public timeline.
        await _auditService.LogAsync(staffUserId, "Received", "FoundItem", itemId.ToString(),
            FoundItemStatus.PendingDropoff.ToString(), FoundItemStatus.Open.ToString(),
            "Nhân viên đã tiếp nhận đồ ký gửi và mở để người mất nhận lại.", isPublic: true);

        await _notificationService.PushAsync(item.ReporterUserId, "ItemReceived",
            "Đồ của bạn đã được tiếp nhận",
            $"Đồ \"{item.Title}\" đã được nhân viên tiếp nhận và đang mở để người mất nhận lại.",
            $"/FoundItems/Details/{itemId}");

        await tx.CommitAsync();
        return true;
    }

    public async Task<StoredItemsViewModel> GetStoredAsync(FoundItemStatus? status)
    {
        var pending = (int)FoundItemStatus.PendingDropoff;
        var query = _db.FoundItem.Where(f => f.CustodianStaffId != null && f.Status != pending);
        if (status.HasValue)
            query = query.Where(f => f.Status == (int)status.Value);

        var items = (await query
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    f.Id,
                    f.Title,
                    f.Status,
                    f.StorageLocation,
                    Custodian = f.CustodianStaff!.FullName ?? f.CustodianStaff.Email,
                    Reporter = f.ReporterUser.FullName ?? f.ReporterUser.Email,
                    f.CreatedAt
                })
                .ToListAsync())
            .Select(x => new StoredItemViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Status = (FoundItemStatus)x.Status,
                StatusText = StatusText(x.Status),
                StorageLocation = x.StorageLocation,
                CustodianName = x.Custodian ?? "?",
                ReporterName = x.Reporter ?? "?",
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return new StoredItemsViewModel { Items = items, StatusFilter = status };
    }

    public async Task<bool> UpdateStorageLocationAsync(int itemId, string storageLocation, string staffUserId)
    {
        var item = await _db.FoundItem.FirstOrDefaultAsync(f => f.Id == itemId);
        if (item is null || item.CustodianStaffId is null) return false; // only items actually in custody

        await using var tx = await _db.Database.BeginTransactionAsync();

        var old = item.StorageLocation;
        item.StorageLocation = storageLocation.Trim();
        await _db.SaveChangesAsync();

        // Internal (isPublic: false): the storage location never belongs on the item's public timeline.
        await _auditService.LogAsync(staffUserId, "StorageUpdated", "FoundItem", itemId.ToString(),
            null, null, $"Cập nhật nơi cất: {old} → {item.StorageLocation}", isPublic: false);

        await tx.CommitAsync();
        return true;
    }

    private static string StatusText(int status) => (FoundItemStatus)status switch
    {
        FoundItemStatus.PendingDropoff => "Chờ tiếp nhận",
        FoundItemStatus.Open => "Đang mở",
        FoundItemStatus.ClaimAccepted => "Đã duyệt nhận",
        FoundItemStatus.Returned => "Đã trả",
        FoundItemStatus.Unclaimed => "Chưa có người nhận",
        FoundItemStatus.Disposed => "Đã thanh lý",
        _ => "?"
    };
}
