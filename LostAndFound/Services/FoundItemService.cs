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
    private const int MaxImages = 7; // max photos per post

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
        // Enforce the per-post image cap BEFORE uploading (don't waste Cloudinary calls).
        var intendedCount = (vm.CoverImage != null ? 1 : 0) + (vm.OtherImages?.Count ?? 0);
        if (intendedCount > MaxImages)
            throw new ImageUploadException($"Tối đa {MaxImages} ảnh mỗi bài.");

        // Upload BEFORE opening the DB transaction (network I/O must not hold a tx open).
        // Cover first (SortOrder 0), then the "other" images.
        var imageUrls = await UploadOrderedAsync(vm.CoverImage, vm.OtherImages);

        // SelfHeld goes public immediately; Custodial waits for staff intake (FR-HOLD-02).
        var status = vm.HoldingType == HoldingType.Custodial ? FoundItemStatus.PendingDropoff : FoundItemStatus.Open;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var item = new FoundItem
        {
            Title = vm.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
            CategoryId = vm.CategoryId!.Value,
            LocationId = vm.LocationId!.Value,
            FoundAt = AppTime.ToUtc(vm.FoundAt!.Value), // form is local wall-clock -> store UTC
            Status = (int)status,
            HoldingType = (int)vm.HoldingType,
            PrivateMarks = string.IsNullOrWhiteSpace(vm.PrivateMarks) ? null : vm.PrivateMarks.Trim(),
            ReporterUserId = reporterUserId
            // CreatedAt is store-generated.
        };
        _db.FoundItem.Add(item);
        await _db.SaveChangesAsync(); // get item.Id

        for (int i = 0; i < imageUrls.Count; i++)
            _db.FoundItemImage.Add(new FoundItemImage { FoundItemId = item.Id, Url = imageUrls[i], SortOrder = i });
        if (imageUrls.Count > 0) await _db.SaveChangesAsync();

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
        // Filter bounds are local wall-clock -> convert to UTC to match the stored FoundAt.
        if (q.FoundFrom is DateTime from) { var fromUtc = AppTime.ToUtc(from); query = query.Where(f => f.FoundAt >= fromUtc); }
        if (q.FoundTo is DateTime to) { var toUtc = AppTime.ToUtc(to).AddMinutes(1); query = query.Where(f => f.FoundAt < toUtc); } // inclusive of the "to" minute
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
                Description = f.Description,
                CategoryName = f.Category.Name,
                LocationName = f.Location.Name,
                FoundAt = f.FoundAt,
                CoverImagePath = f.FoundItemImage.OrderBy(im => im.SortOrder).Select(im => im.Url).FirstOrDefault(),
                ImageCount = f.FoundItemImage.Count,
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
            .Include(f => f.FoundItemImage)
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

        // Owner may edit/delete only while the item is still editable (no claim yet).
        var hasClaim = await _db.Claim.AsNoTracking().AnyAsync(c => c.FoundItemId == id);
        var canEdit = isReporter
            && (status == FoundItemStatus.Open || status == FoundItemStatus.PendingDropoff)
            && !hasClaim;

        var reporterName = await _db.Users.AsNoTracking()
            .Where(u => u.Id == item.ReporterUserId)
            .Select(u => u.FullName ?? u.Email)
            .FirstOrDefaultAsync() ?? "N/A";

        // Left-join Users for the actor's display name (ActorUserId is a plain FK, no navigation).
        var events = await (
            from a in _db.AuditLog.AsNoTracking()
            join usr in _db.Users.AsNoTracking() on a.ActorUserId equals usr.Id into au
            from usr in au.DefaultIfEmpty()
            where a.EntityType == "FoundItem" && a.EntityId == id.ToString() && a.IsPublic
            orderby a.CreatedAt
            select new FoundItemDetailViewModel.PublicEvent
            {
                At = a.CreatedAt,
                Action = a.Action,
                ToStatus = a.ToStatus,
                Detail = a.Detail,
                ActorName = usr != null ? (usr.FullName ?? usr.Email) : null
            }).ToListAsync();

        return new FoundItemDetailViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            CategoryName = item.Category.Name,
            LocationName = item.Location.Name,
            FoundAt = item.FoundAt,
            ImagePaths = item.FoundItemImage.OrderBy(im => im.SortOrder).Select(im => im.Url).ToList(),
            ReporterName = reporterName,
            HoldingType = (HoldingType)item.HoldingType,
            Status = status,
            DisplayTags = item.FoundItemTag.Select(ft => ft.Tag.DisplayTag).ToList(),
            CanSeePrivate = canSee,
            PrivateMarks = canSee ? item.PrivateMarks : null,
            StorageLocation = canSee ? item.StorageLocation : null,
            CanEdit = canEdit,
            PublicEvents = events
        };
    }

    /// <inheritdoc />
    public async Task<FoundItemEditViewModel?> GetForEditAsync(int id, string userId)
    {
        var item = await _db.FoundItem.AsNoTracking()
            .Include(f => f.FoundItemTag).ThenInclude(ft => ft.Tag)
            .Include(f => f.FoundItemImage)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (item is null || item.ReporterUserId != userId) return null; // owner-only
        if (!await IsEditableAsync(item)) return null;

        return new FoundItemEditViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            CategoryId = item.CategoryId,
            LocationId = item.LocationId,
            FoundAt = AppTime.ToLocal(item.FoundAt), // stored UTC -> local for the datetime-local input
            PrivateMarks = item.PrivateMarks,
            TagsRaw = string.Join(", ", item.FoundItemTag.Select(ft => ft.Tag.DisplayTag)),
            ExistingImages = item.FoundItemImage
                .OrderBy(im => im.SortOrder)
                .Select(im => new FoundItemEditViewModel.ImageItem { Id = im.Id, Url = im.Url })
                .ToList(),
            HoldingType = (HoldingType)item.HoldingType
        };
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(int id, FoundItemEditViewModel vm, string userId)
    {
        var item = await _db.FoundItem
            .Include(f => f.FoundItemTag)
            .Include(f => f.FoundItemImage)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (item is null || item.ReporterUserId != userId) return false; // owner-only
        if (!await IsEditableAsync(item)) return false;

        // Enforce the per-post image cap (kept + newly added) BEFORE uploading.
        var removeIds = vm.RemoveImageIds ?? new List<int>();
        var keptCount = item.FoundItemImage.Count(im => !removeIds.Contains(im.Id));
        if (keptCount + (vm.NewImages?.Count ?? 0) > MaxImages)
            throw new ImageUploadException($"Tối đa {MaxImages} ảnh mỗi bài.");

        // Upload any newly added images before the transaction.
        var newUrls = new List<string>();
        if (vm.NewImages is not null)
            foreach (var f in vm.NewImages)
            {
                var u = await _images.UploadAsync(f, ImageFolder);
                if (u is not null) newUrls.Add(u);
            }

        await using var tx = await _db.Database.BeginTransactionAsync();

        item.Title = vm.Title.Trim();
        item.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
        item.CategoryId = vm.CategoryId!.Value;
        item.LocationId = vm.LocationId!.Value;
        item.FoundAt = AppTime.ToUtc(vm.FoundAt!.Value); // form is local wall-clock -> store UTC
        item.PrivateMarks = string.IsNullOrWhiteSpace(vm.PrivateMarks) ? null : vm.PrivateMarks.Trim();

        // Images: drop the ticked ones, keep the rest, append the new ones, renumber SortOrder (cover = 0).
        var toRemove = item.FoundItemImage.Where(im => removeIds.Contains(im.Id)).ToList();
        if (toRemove.Count > 0) _db.FoundItemImage.RemoveRange(toRemove);
        var kept = item.FoundItemImage.Where(im => !removeIds.Contains(im.Id)).OrderBy(im => im.SortOrder).ToList();
        var order = 0;
        foreach (var im in kept) im.SortOrder = order++;
        foreach (var url in newUrls)
            _db.FoundItemImage.Add(new FoundItemImage { FoundItemId = item.Id, Url = url, SortOrder = order++ });

        // Replace the tag set: delete old joins first (avoid a unique-key clash), then add the resolved tags.
        _db.FoundItemTag.RemoveRange(item.FoundItemTag);
        await _db.SaveChangesAsync();

        var tags = await _tags.ResolveTagsAsync(vm.TagList);
        foreach (var tag in tags)
            _db.FoundItemTag.Add(new FoundItemTag { FoundItemId = item.Id, Tag = tag });
        await _db.SaveChangesAsync();

        // Public so the edit shows in the item's timeline. Detail is a generic label — never field values.
        await _audit.LogAsync(userId, "Updated", "FoundItem", item.Id.ToString(),
            null, null, "Cập nhật bài đăng", isPublic: true);

        await tx.CommitAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var item = await _db.FoundItem.FirstOrDefaultAsync(f => f.Id == id);
        if (item is null || item.ReporterUserId != userId) return false; // owner-only
        if (!await IsEditableAsync(item)) return false;

        await using var tx = await _db.Database.BeginTransactionAsync();
        _db.FoundItem.Remove(item); // FoundItemTag rows cascade
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "Deleted", "FoundItem", id.ToString(),
            null, null, "Xoá bài đăng", isPublic: false);

        await tx.CommitAsync();
        return true;
    }

    /// <summary>Uploads the cover (first) then the other images, returning URLs in cover-first order.</summary>
    private async Task<List<string>> UploadOrderedAsync(IFormFile? cover, List<IFormFile>? others)
    {
        var urls = new List<string>();
        var coverUrl = await _images.UploadAsync(cover, ImageFolder);
        if (coverUrl is not null) urls.Add(coverUrl);
        if (others is not null)
            foreach (var f in others)
            {
                var u = await _images.UploadAsync(f, ImageFolder);
                if (u is not null) urls.Add(u);
            }
        return urls;
    }

    /// <summary>Editable only while Open/PendingDropoff AND no claim exists yet.</summary>
    private async Task<bool> IsEditableAsync(FoundItem item)
    {
        var status = (FoundItemStatus)item.Status;
        if (status != FoundItemStatus.Open && status != FoundItemStatus.PendingDropoff) return false;
        return !await _db.Claim.AsNoTracking().AnyAsync(c => c.FoundItemId == item.Id);
    }
}
