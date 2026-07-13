using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Common;
using LostAndFound.Models.ViewModels.FoundItems;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

/// <summary>See <see cref="IFoundItemService"/>. Thin-controller partner: all FR-FOUND rules live here.</summary>
public class FoundItemService : IFoundItemService
{
    private const string ImageFolder = "lostandfound/founditems";
    private const int MaxPageSize = 60;

    private readonly ApplicationDbContext _db;
    private readonly ITagService _tags;
    private readonly IAuditService _audit;
    private readonly IImageUploadService _images;

    public FoundItemService(ApplicationDbContext db, ITagService tags, IAuditService audit, IImageUploadService images)
    {
        _db = db;
        _tags = tags;
        _audit = audit;
        _images = images;
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(FoundItemCreateViewModel vm, string reporterUserId)
    {
        // Upload BEFORE opening the DB transaction (network I/O must not hold a tx open).
        var imageUrl = await _images.UploadAsync(vm.ImageFile, ImageFolder);

        // SelfHeld goes public immediately; Custodial waits for staff intake (FR-HOLD-02).
        var status = vm.HoldingType == HoldingType.Custodial ? FoundItemStatus.PendingDropoff : FoundItemStatus.Open;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var item = new FoundItem
        {
            Title = vm.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
            CategoryId = vm.CategoryId!.Value,
            LocationId = vm.LocationId!.Value,
            FoundAt = vm.FoundAt!.Value,
            Status = (int)status,
            HoldingType = (int)vm.HoldingType,
            PrivateMarks = string.IsNullOrWhiteSpace(vm.PrivateMarks) ? null : vm.PrivateMarks.Trim(),
            ImagePath = imageUrl,
            ReporterUserId = reporterUserId
            // CreatedAt is store-generated.
        };
        _db.FoundItem.Add(item);
        await _db.SaveChangesAsync(); // get item.Id

        var tags = await _tags.ResolveTagsAsync(vm.TagList);
        foreach (var tag in tags)
        {
            // Use the navigation so tags created in this same transaction (Id still 0) get linked correctly.
            _db.FoundItemTag.Add(new FoundItemTag { FoundItemId = item.Id, Tag = tag });
        }
        if (tags.Any()) await _db.SaveChangesAsync();

        await _audit.LogAsync(
            actorUserId: reporterUserId,
            action: "Created",
            entityType: "FoundItem",
            entityId: item.Id.ToString(),
            fromStatus: null,
            toStatus: status.ToString(),
            detail: $"Đăng đồ nhặt được: {item.Title}", // title is public; never PrivateMarks
            isPublic: status == FoundItemStatus.Open);

        await tx.CommitAsync();
        return item.Id;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FoundItemListItemViewModel>> SearchAsync(FoundItemSearchViewModel q)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 12 : Math.Min(q.PageSize, MaxPageSize);

        // Public list shows ONLY Open items.
        var query = _db.FoundItem.AsNoTracking().Where(f => f.Status == (int)FoundItemStatus.Open);

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var kw = q.Keyword.Trim();
            query = query.Where(f => f.Title.Contains(kw) || (f.Description != null && f.Description.Contains(kw)));
        }
        if (q.CategoryId is int cat) query = query.Where(f => f.CategoryId == cat);
        if (q.LocationId is int loc) query = query.Where(f => f.LocationId == loc);
        if (!string.IsNullOrWhiteSpace(q.Tag))
        {
            var norm = _tags.Normalize(q.Tag);
            query = query.Where(f => f.FoundItemTag.Any(ft => ft.Tag.NormalizedTag == norm));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FoundItemListItemViewModel
            {
                Id = f.Id,
                Title = f.Title,
                CategoryName = f.Category.Name,
                LocationName = f.Location.Name,
                FoundAt = f.FoundAt,
                ImagePath = f.ImagePath,
                CreatedAt = f.CreatedAt,
                DisplayTags = f.FoundItemTag.Select(ft => ft.Tag.DisplayTag).ToList()
                // NO PrivateMarks selected — blind listing.
            })
            .ToListAsync();

        return new PagedResult<FoundItemListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    /// <inheritdoc />
    public async Task<FoundItemDetailViewModel?> GetDetailAsync(int id, ClaimsPrincipal user)
    {
        var item = await _db.FoundItem.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Location)
            .Include(f => f.FoundItemTag).ThenInclude(ft => ft.Tag)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (item is null) return null;

        var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var isReporter = uid != null && uid == item.ReporterUserId;
        var isCustodian = uid != null && item.CustodianStaffId != null && uid == item.CustodianStaffId;
        var isStaffish = user.IsInRole("Staff") || user.IsInRole("Admin");
        var canSee = isReporter || isCustodian || isStaffish;

        var status = (FoundItemStatus)item.Status;
        // Non-Open items (e.g. Custodial PendingDropoff) are invisible to the public.
        if (status != FoundItemStatus.Open && !canSee) return null;

        var reporterName = await _db.Users.AsNoTracking()
            .Where(u => u.Id == item.ReporterUserId)
            .Select(u => u.FullName ?? u.Email)
            .FirstOrDefaultAsync() ?? "N/A";

        var events = await _db.AuditLog.AsNoTracking()
            .Where(a => a.EntityType == "FoundItem" && a.EntityId == id.ToString() && a.IsPublic)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new FoundItemDetailViewModel.PublicEvent
            {
                At = a.CreatedAt,
                Action = a.Action,
                ToStatus = a.ToStatus,
                Detail = a.Detail
            })
            .ToListAsync();

        return new FoundItemDetailViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            CategoryName = item.Category.Name,
            LocationName = item.Location.Name,
            FoundAt = item.FoundAt,
            ImagePath = item.ImagePath,
            ReporterName = reporterName,
            HoldingType = (HoldingType)item.HoldingType,
            Status = status,
            DisplayTags = item.FoundItemTag.Select(ft => ft.Tag.DisplayTag).ToList(),
            CanSeePrivate = canSee,
            PrivateMarks = canSee ? item.PrivateMarks : null,
            StorageLocation = canSee ? item.StorageLocation : null,
            PublicEvents = events
        };
    }
}
