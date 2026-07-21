using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.FoundItems;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

public class FoundItemService : IFoundItemService
{
    private const string ImageFolder = "lostandfound/founditems";
    private const int MaxImages = 7;

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

    public async Task<int> CreateAsync(FoundItemCreateViewModel vm, string reporterUserId)
    {

        var poster = await _db.Users.FindAsync(reporterUserId);
        if (poster is not null && poster.IsPostingBlocked)
            throw new InvalidOperationException("Bạn đã bị chặn đăng bài. Vui lòng liên hệ quản trị viên.");

        var intendedCount = (vm.CoverImage != null ? 1 : 0) + (vm.OtherImages?.Count ?? 0);
        if (intendedCount > MaxImages)
            throw new ImageUploadException($"Tối đa {MaxImages} ảnh mỗi bài.");

        var imageUrls = await UploadOrderedAsync(vm.CoverImage, vm.OtherImages);

        var status = vm.HoldingType == HoldingType.Custodial ? FoundItemStatus.PendingDropoff : FoundItemStatus.Open;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var item = new FoundItem
        {
            Title = vm.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
            CategoryId = vm.CategoryId!.Value,
            LocationId = vm.LocationId!.Value,
            FoundAt = AppTime.ToUtc(vm.FoundAt!.Value),
            Status = (int)status,
            HoldingType = (int)vm.HoldingType,
            PrivateMarks = string.IsNullOrWhiteSpace(vm.PrivateMarks) ? null : vm.PrivateMarks.Trim(),
            ReporterUserId = reporterUserId

        };
        _db.FoundItem.Add(item);
        await _db.SaveChangesAsync();

        for (int i = 0; i < imageUrls.Count; i++)
            _db.FoundItemImage.Add(new FoundItemImage { FoundItemId = item.Id, Url = imageUrls[i], SortOrder = i });
        if (imageUrls.Count > 0) await _db.SaveChangesAsync();

        var tags = await _tags.ResolveTagsAsync(vm.TagList);
        foreach (var tag in tags)
        {

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
            detail: $"Đăng đồ nhặt được: {item.Title}",
            isPublic: status == FoundItemStatus.Open);

        await tx.CommitAsync();
        return item.Id;
    }

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

        var canSeePrivate = isReporter || isCustodian || isStaffish;

        var isClaimant = uid != null && await _db.Claim.AsNoTracking()
            .AnyAsync(c => c.FoundItemId == id && c.ClaimantUserId == uid);

        var status = (FoundItemStatus)item.Status;

        var isPubliclyViewable = status == FoundItemStatus.Open || status == FoundItemStatus.Returned;
        if (!isPubliclyViewable && !canSeePrivate && !isClaimant) return null;

        var hasClaim = await _db.Claim.AsNoTracking().AnyAsync(c => c.FoundItemId == id);
        var canEdit = isReporter
            && (status == FoundItemStatus.Open || status == FoundItemStatus.PendingDropoff)
            && !hasClaim;

        var reporterName = await _db.Users.AsNoTracking()
            .Where(u => u.Id == item.ReporterUserId)
            .Select(u => u.FullName ?? u.Email)
            .FirstOrDefaultAsync() ?? "N/A";

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
            CanSeePrivate = canSeePrivate,
            PrivateMarks = canSeePrivate ? item.PrivateMarks : null,
            StorageLocation = canSeePrivate ? item.StorageLocation : null,
            CanEdit = canEdit,
            PublicEvents = events
        };
    }

    public async Task<FoundItemEditViewModel?> GetForEditAsync(int id, string userId)
    {
        var item = await _db.FoundItem.AsNoTracking()
            .Include(f => f.FoundItemTag).ThenInclude(ft => ft.Tag)
            .Include(f => f.FoundItemImage)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (item is null || item.ReporterUserId != userId) return null;
        if (!await IsEditableAsync(item)) return null;

        return new FoundItemEditViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            CategoryId = item.CategoryId,
            LocationId = item.LocationId,
            FoundAt = AppTime.ToLocal(item.FoundAt),
            PrivateMarks = item.PrivateMarks,
            TagsRaw = string.Join(", ", item.FoundItemTag.Select(ft => ft.Tag.DisplayTag)),
            ExistingImages = item.FoundItemImage
                .OrderBy(im => im.SortOrder)
                .Select(im => new FoundItemEditViewModel.ImageItem { Id = im.Id, Url = im.Url })
                .ToList(),
            CoverImageId = item.FoundItemImage.OrderBy(im => im.SortOrder).Select(im => (int?)im.Id).FirstOrDefault(),
            HoldingType = (HoldingType)item.HoldingType
        };
    }

    public async Task<bool> UpdateAsync(int id, FoundItemEditViewModel vm, string userId)
    {
        var item = await _db.FoundItem
            .Include(f => f.FoundItemTag)
            .Include(f => f.FoundItemImage)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (item is null || item.ReporterUserId != userId) return false;
        if (!await IsEditableAsync(item)) return false;

        var removeIds = vm.RemoveImageIds ?? new List<int>();
        var keptCount = item.FoundItemImage.Count(im => !removeIds.Contains(im.Id));
        if (keptCount + (vm.NewImages?.Count ?? 0) > MaxImages)
            throw new ImageUploadException($"Tối đa {MaxImages} ảnh mỗi bài.");

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
        item.FoundAt = AppTime.ToUtc(vm.FoundAt!.Value);
        item.PrivateMarks = string.IsNullOrWhiteSpace(vm.PrivateMarks) ? null : vm.PrivateMarks.Trim();

        var toRemove = item.FoundItemImage.Where(im => removeIds.Contains(im.Id)).ToList();
        if (toRemove.Count > 0) _db.FoundItemImage.RemoveRange(toRemove);
        var kept = item.FoundItemImage.Where(im => !removeIds.Contains(im.Id)).OrderBy(im => im.SortOrder).ToList();

        if (vm.CoverImageId is int coverId)
        {
            var cover = kept.FirstOrDefault(im => im.Id == coverId);
            if (cover is not null) { kept.Remove(cover); kept.Insert(0, cover); }
        }
        var order = 0;
        foreach (var im in kept) im.SortOrder = order++;
        foreach (var url in newUrls)
            _db.FoundItemImage.Add(new FoundItemImage { FoundItemId = item.Id, Url = url, SortOrder = order++ });

        _db.FoundItemTag.RemoveRange(item.FoundItemTag);
        await _db.SaveChangesAsync();

        var tags = await _tags.ResolveTagsAsync(vm.TagList);
        foreach (var tag in tags)
            _db.FoundItemTag.Add(new FoundItemTag { FoundItemId = item.Id, Tag = tag });
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "Updated", "FoundItem", item.Id.ToString(),
            null, null, "Cập nhật bài đăng", isPublic: true);

        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var item = await _db.FoundItem.FirstOrDefaultAsync(f => f.Id == id);
        if (item is null || item.ReporterUserId != userId) return false;
        if (!await IsEditableAsync(item)) return false;

        await using var tx = await _db.Database.BeginTransactionAsync();
        _db.FoundItem.Remove(item);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "Deleted", "FoundItem", id.ToString(),
            null, null, "Xoá bài đăng", isPublic: false);

        await tx.CommitAsync();
        return true;
    }

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

    private async Task<bool> IsEditableAsync(FoundItem item)
    {
        var status = (FoundItemStatus)item.Status;
        if (status != FoundItemStatus.Open && status != FoundItemStatus.PendingDropoff) return false;
        return !await _db.Claim.AsNoTracking().AnyAsync(c => c.FoundItemId == item.Id);
    }
}
