using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.LostItems;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

/// <summary>See <see cref="ILostItemService"/>. Mirrors FoundItemService for the lost-item board.</summary>
public class LostItemService : ILostItemService
{
    private const string ImageFolder = "lostandfound/lostitems";
    private const int MaxImages = 7;

    private readonly ApplicationDbContext _db;
    private readonly ITagService _tags;
    private readonly IAuditService _audit;
    private readonly IImageUploadService _images;

    public LostItemService(ApplicationDbContext db, ITagService tags, IAuditService audit, IImageUploadService images)
    {
        _db = db;
        _tags = tags;
        _audit = audit;
        _images = images;
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(LostItemCreateViewModel vm, string ownerUserId)
    {
        // Backstop: an admin may have flagged this user IsPostingBlocked. Controllers pre-check this,
        // but enforce it here too so no create path (present or future) can slip past the block.
        var poster = await _db.Users.FindAsync(ownerUserId);
        if (poster is not null && poster.IsPostingBlocked)
            throw new InvalidOperationException("Bạn đã bị chặn đăng bài. Vui lòng liên hệ quản trị viên.");

        var intended = (vm.CoverImage != null ? 1 : 0) + (vm.OtherImages?.Count ?? 0);
        if (intended > MaxImages) throw new ImageUploadException($"Tối đa {MaxImages} ảnh mỗi bài.");

        var imageUrls = await UploadOrderedAsync(vm.CoverImage, vm.OtherImages);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var item = new LostItem
        {
            Title = vm.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
            CategoryId = vm.CategoryId!.Value,
            LocationId = vm.LocationId!.Value,
            LostAt = AppTime.ToUtc(vm.LostAt!.Value),
            Status = (int)LostItemStatus.Open,
            OwnerUserId = ownerUserId
        };
        _db.LostItem.Add(item);
        await _db.SaveChangesAsync();

        for (int i = 0; i < imageUrls.Count; i++)
            _db.LostItemImage.Add(new LostItemImage { LostItemId = item.Id, Url = imageUrls[i], SortOrder = i });
        if (imageUrls.Count > 0) await _db.SaveChangesAsync();

        var tags = await _tags.ResolveTagsAsync(vm.TagList);
        foreach (var tag in tags)
            _db.LostItemTag.Add(new LostItemTag { LostItemId = item.Id, Tag = tag });
        if (tags.Any()) await _db.SaveChangesAsync();

        await _audit.LogAsync(ownerUserId, "Created", "LostItem", item.Id.ToString(),
            null, LostItemStatus.Open.ToString(), $"Đăng đồ bị mất: {item.Title}", isPublic: true);

        await tx.CommitAsync();
        return item.Id;
    }

    /// <inheritdoc />
    public async Task<LostItemDetailViewModel?> GetDetailAsync(int id, ClaimsPrincipal user)
    {
        var item = await _db.LostItem.AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.Location)
            .Include(l => l.LostItemTag).ThenInclude(lt => lt.Tag)
            .Include(l => l.LostItemImage)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (item is null) return null;

        var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var isOwner = uid != null && uid == item.OwnerUserId;
        var isStaffish = user.IsInRole("Staff") || user.IsInRole("Admin");
        var status = (LostItemStatus)item.Status;

        // Non-Open posts are only visible to the owner/staff.
        if (status != LostItemStatus.Open && !(isOwner || isStaffish)) return null;

        var ownerName = await _db.Users.AsNoTracking()
            .Where(u => u.Id == item.OwnerUserId).Select(u => u.FullName ?? u.Email).FirstOrDefaultAsync() ?? "N/A";

        var events = await (
            from a in _db.AuditLog.AsNoTracking()
            join usr in _db.Users.AsNoTracking() on a.ActorUserId equals usr.Id into au
            from usr in au.DefaultIfEmpty()
            where a.EntityType == "LostItem" && a.EntityId == id.ToString() && a.IsPublic
            orderby a.CreatedAt
            select new LostItemDetailViewModel.PublicEvent
            {
                At = a.CreatedAt,
                Action = a.Action,
                Detail = a.Detail,
                ActorName = usr != null ? (usr.FullName ?? usr.Email) : null
            }).ToListAsync();

        return new LostItemDetailViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            CategoryName = item.Category.Name,
            LocationName = item.Location.Name,
            LostAt = item.LostAt,
            ImagePaths = item.LostItemImage.OrderBy(im => im.SortOrder).Select(im => im.Url).ToList(),
            OwnerName = ownerName,
            Status = status,
            DisplayTags = item.LostItemTag.Select(lt => lt.Tag.DisplayTag).ToList(),
            CanEdit = isOwner && status == LostItemStatus.Open,
            PublicEvents = events
        };
    }

    /// <inheritdoc />
    public async Task<LostItemEditViewModel?> GetForEditAsync(int id, string userId)
    {
        var item = await _db.LostItem.AsNoTracking()
            .Include(l => l.LostItemTag).ThenInclude(lt => lt.Tag)
            .Include(l => l.LostItemImage)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (item is null || item.OwnerUserId != userId) return null;
        if ((LostItemStatus)item.Status != LostItemStatus.Open) return null;

        return new LostItemEditViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            CategoryId = item.CategoryId,
            LocationId = item.LocationId,
            LostAt = AppTime.ToLocal(item.LostAt),
            TagsRaw = string.Join(", ", item.LostItemTag.Select(lt => lt.Tag.DisplayTag)),
            ExistingImages = item.LostItemImage.OrderBy(im => im.SortOrder)
                .Select(im => new LostItemEditViewModel.ImageItem { Id = im.Id, Url = im.Url }).ToList(),
            CoverImageId = item.LostItemImage.OrderBy(im => im.SortOrder).Select(im => (int?)im.Id).FirstOrDefault()
        };
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(int id, LostItemEditViewModel vm, string userId)
    {
        var item = await _db.LostItem
            .Include(l => l.LostItemTag)
            .Include(l => l.LostItemImage)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (item is null || item.OwnerUserId != userId) return false;
        if ((LostItemStatus)item.Status != LostItemStatus.Open) return false;

        var removeIds = vm.RemoveImageIds ?? new List<int>();
        var keptCount = item.LostItemImage.Count(im => !removeIds.Contains(im.Id));
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
        item.LostAt = AppTime.ToUtc(vm.LostAt!.Value);

        var toRemove = item.LostItemImage.Where(im => removeIds.Contains(im.Id)).ToList();
        if (toRemove.Count > 0) _db.LostItemImage.RemoveRange(toRemove);
        var kept = item.LostItemImage.Where(im => !removeIds.Contains(im.Id)).OrderBy(im => im.SortOrder).ToList();
        // Float the explicitly-chosen cover (if it survived removal) to SortOrder 0.
        if (vm.CoverImageId is int coverId)
        {
            var cover = kept.FirstOrDefault(im => im.Id == coverId);
            if (cover is not null) { kept.Remove(cover); kept.Insert(0, cover); }
        }
        var order = 0;
        foreach (var im in kept) im.SortOrder = order++;
        foreach (var url in newUrls)
            _db.LostItemImage.Add(new LostItemImage { LostItemId = item.Id, Url = url, SortOrder = order++ });

        _db.LostItemTag.RemoveRange(item.LostItemTag);
        await _db.SaveChangesAsync();

        var tags = await _tags.ResolveTagsAsync(vm.TagList);
        foreach (var tag in tags)
            _db.LostItemTag.Add(new LostItemTag { LostItemId = item.Id, Tag = tag });
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "Updated", "LostItem", item.Id.ToString(),
            null, null, "Cập nhật bài đăng", isPublic: true);

        await tx.CommitAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var item = await _db.LostItem.FirstOrDefaultAsync(l => l.Id == id);
        if (item is null || item.OwnerUserId != userId) return false;
        if ((LostItemStatus)item.Status != LostItemStatus.Open) return false;

        await using var tx = await _db.Database.BeginTransactionAsync();
        _db.LostItem.Remove(item); // images + tag links cascade
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "Deleted", "LostItem", id.ToString(), null, null, "Xoá bài đăng", isPublic: false);
        await tx.CommitAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkResolvedAsync(int id, string userId)
    {
        var item = await _db.LostItem.FirstOrDefaultAsync(l => l.Id == id);
        if (item is null || item.OwnerUserId != userId) return false;
        if ((LostItemStatus)item.Status != LostItemStatus.Open) return false;

        await using var tx = await _db.Database.BeginTransactionAsync();
        item.Status = (int)LostItemStatus.Resolved;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "Resolved", "LostItem", id.ToString(),
            LostItemStatus.Open.ToString(), LostItemStatus.Resolved.ToString(), "Đã tìm thấy", isPublic: true);
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
}
