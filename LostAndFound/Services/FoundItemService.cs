using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.FoundItems;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

/// <summary>See <see cref="IFoundItemService"/>. Thin-controller partner: all FR-FOUND rules live here.</summary>
public class FoundItemService : IFoundItemService
{
    private const string ImageFolder = "lostandfound/founditems";
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
        // Backstop: an admin may have flagged this user IsPostingBlocked. Controllers pre-check this,
        // but enforce it here too so no create path (present or future) can slip past the block.
        var poster = await _db.Users.FindAsync(reporterUserId);
        if (poster is not null && poster.IsPostingBlocked)
            throw new InvalidOperationException("Bạn đã bị chặn đăng bài. Vui lòng liên hệ quản trị viên.");

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

        // These are TWO different questions and must not share one flag:
        //  (1) may this viewer read the hidden fields (PrivateMarks / StorageLocation)?
        //  (2) may this viewer open the page at all?
        // The accepted claimant answers YES to (2) and NO to (1) — PrivateMarks is the secret they were
        // verified against, so handing it to them would gut the whole two-sided check.
        var canSeePrivate = isReporter || isCustodian || isStaffish;

        // A party to the item: needed during ClaimAccepted (the handover panel) and after Returned —
        // ConfirmReceived redirects straight here, so without this the claimant 404s the instant they
        // confirm receipt of their own item.
        var isAcceptedClaimant = uid != null && await _db.Claim.AsNoTracking()
            .AnyAsync(c => c.FoundItemId == id && c.ClaimantUserId == uid && c.Status == (int)ClaimStatus.Accepted);

        var status = (FoundItemStatus)item.Status;
        // Publicly viewable: Open (it's browsable) and Returned — FR-TL-03 finalises the timeline there and
        // FR-THANK-02 puts the thank-you on this very page, and a success story nobody can open is pointless.
        // Returned is still absent from the BOARD (that lists Open only); this is about the detail page.
        // Everything else (PendingDropoff / ClaimAccepted / Unclaimed / Disposed) stays with the parties + staff.
        // PrivateMarks is gated separately by canSeePrivate, so opening the page never exposes the secret.
        var isPubliclyViewable = status == FoundItemStatus.Open || status == FoundItemStatus.Returned;
        if (!isPubliclyViewable && !canSeePrivate && !isAcceptedClaimant) return null;

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
            CanSeePrivate = canSeePrivate,
            PrivateMarks = canSeePrivate ? item.PrivateMarks : null,
            StorageLocation = canSeePrivate ? item.StorageLocation : null,
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
            CoverImageId = item.FoundItemImage.OrderBy(im => im.SortOrder).Select(im => (int?)im.Id).FirstOrDefault(),
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
        // Float the explicitly-chosen cover (if it survived removal) to SortOrder 0.
        if (vm.CoverImageId is int coverId)
        {
            var cover = kept.FirstOrDefault(im => im.Id == coverId);
            if (cover is not null) { kept.Remove(cover); kept.Insert(0, cover); }
        }
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
