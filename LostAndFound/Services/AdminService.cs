using LostAndFound.Data;
using LostAndFound.Models.Entities;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Admin;
using LostAndFound.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public AdminService(ApplicationDbContext db, IAuditService auditService, INotificationService notificationService)
    {
        _db = db;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    #region Category Management

    public async Task<List<CategoryViewModel>> GetAllCategoriesAsync()
    {
        var categories = await _db.Category
            .Include(c => c.InverseParent)
            .Include(c => c.FoundItem)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var parents = categories.Where(c => c.ParentId == null).OrderBy(c => c.Name).ToList();
        var result = new List<CategoryViewModel>();

        foreach (var parent in parents)
        {
            // Add parent category
            result.Add(new CategoryViewModel
            {
                Id = parent.Id,
                Name = parent.Name,
                ParentId = parent.ParentId,
                ParentName = parent.Parent?.Name,
                HasChildren = parent.InverseParent.Any(),
                ItemCount = parent.FoundItem.Count,
                Level = 0 // Root level
            });

            // Add children categories (nested under parent)
            var children = categories.Where(c => c.ParentId == parent.Id).OrderBy(c => c.Name).ToList();
            foreach (var child in children)
            {
                result.Add(new CategoryViewModel
                {
                    Id = child.Id,
                    Name = child.Name,
                    ParentId = child.ParentId,
                    ParentName = child.Parent?.Name,
                    HasChildren = child.InverseParent.Any(),
                    ItemCount = child.FoundItem.Count,
                    Level = 1 // Child level
                });
            }
        }

        return result;
    }

    public Task<CategoryCreateViewModel> GetCategoryCreateViewModelAsync()
        // Parent dropdown is supplied separately by the controller (ViewBag); nothing to load here.
        => Task.FromResult(new CategoryCreateViewModel());

    public async Task<CategoryEditViewModel?> GetCategoryEditViewModelAsync(int id)
    {
        var category = await _db.Category
            .Include(c => c.Parent)
            .Include(c => c.InverseParent)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return null;

        return new CategoryEditViewModel
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            ParentName = category.Parent?.Name,
            HasChildren = category.InverseParent.Any()
        };
    }

    public async Task<int> CreateCategoryAsync(CategoryCreateViewModel model, string actorUserId)
    {
        // Validate ParentId exists if provided
        if (model.ParentId.HasValue)
        {
            var parentExists = await _db.Category.AnyAsync(c => c.Id == model.ParentId.Value);
            if (!parentExists)
                throw new ArgumentException("Parent category not found");
        }

        var category = new Category
        {
            Name = model.Name,
            ParentId = model.ParentId
        };

        _db.Category.Add(category);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Created", "Category", category.Id.ToString(),
            null, null, $"Created category: {category.Name}", isPublic: false);

        return category.Id;
    }

    public async Task<bool> UpdateCategoryAsync(CategoryEditViewModel model, string actorUserId)
    {
        var category = await _db.Category
            .Include(c => c.InverseParent)
            .FirstOrDefaultAsync(c => c.Id == model.Id);

        if (category == null) return false;

        // Prevent circular reference
        if (model.ParentId.HasValue && model.ParentId.Value == model.Id)
            throw new ArgumentException("Danh mục không thể là cha của chính nó");

        // Prevent moving a parent under its own child
        if (model.ParentId.HasValue)
        {
            var isDescendant = IsDescendant(model.ParentId.Value, model.Id);
            if (isDescendant)
                throw new ArgumentException("Không thể chuyển danh mục vào dưới nhánh con của chính nó");
        }

        var oldName = category.Name;
        var oldParentId = category.ParentId;

        category.Name = model.Name;
        category.ParentId = model.ParentId;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Updated", "Category", category.Id.ToString(),
            null, null,
            $"Updated category: {oldName} -> {category.Name}, Parent: {oldParentId} -> {category.ParentId}", isPublic: false);

        return true;
    }

    public async Task<bool> DeleteCategoryAsync(int id, string actorUserId)
    {
        var category = await _db.Category
            .Include(c => c.InverseParent)
            .Include(c => c.FoundItem)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return false;

        // Prevent deletion if has children
        if (category.InverseParent.Any())
            throw new ArgumentException("Không thể xoá danh mục còn danh mục con");

        // Prevent deletion if has items
        if (category.FoundItem.Any())
            throw new ArgumentException("Không thể xoá danh mục đang có đồ gắn vào");

        var categoryName = category.Name;
        _db.Category.Remove(category);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Deleted", "Category", id.ToString(),
            null, null, $"Deleted category: {categoryName}", isPublic: false);

        return true;
    }

    private bool IsDescendant(int potentialParentId, int categoryId)
    {
        var child = _db.Category.Find(categoryId);
        if (child == null) return false;

        var currentParentId = child.ParentId;
        while (currentParentId.HasValue)
        {
            if (currentParentId.Value == potentialParentId) return true;
            var parent = _db.Category.Find(currentParentId.Value);
            if (parent == null) break;
            currentParentId = parent.ParentId;
        }

        return false;
    }

    #endregion

    #region Location Management

    public async Task<List<LocationViewModel>> GetAllLocationsAsync()
    {
        var locations = await _db.Location
            .Include(l => l.FoundItem)
            .OrderBy(l => l.Building)
            .ThenBy(l => l.Name)
            .ToListAsync();

        return locations.Select(l => new LocationViewModel
        {
            Id = l.Id,
            Building = l.Building,
            Name = l.Name,
            ItemCount = l.FoundItem.Count
        }).ToList();
    }

    public Task<LocationCreateViewModel> GetLocationCreateViewModelAsync()
        => Task.FromResult(new LocationCreateViewModel());

    public async Task<LocationEditViewModel?> GetLocationEditViewModelAsync(int id)
    {
        var location = await _db.Location.FindAsync(id);
        if (location == null) return null;

        return new LocationEditViewModel
        {
            Id = location.Id,
            Building = location.Building,
            Name = location.Name
        };
    }

    public async Task<int> CreateLocationAsync(LocationCreateViewModel model, string actorUserId)
    {
        var location = new Location
        {
            Building = model.Building,
            Name = model.Name
        };

        _db.Location.Add(location);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Created", "Location", location.Id.ToString(),
            null, null, $"Created location: {location.Name}", isPublic: false);

        return location.Id;
    }

    public async Task<bool> UpdateLocationAsync(LocationEditViewModel model, string actorUserId)
    {
        var location = await _db.Location.FindAsync(model.Id);
        if (location == null) return false;

        var oldName = location.Name;
        var oldBuilding = location.Building;

        location.Building = model.Building;
        location.Name = model.Name;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Updated", "Location", location.Id.ToString(),
            null, null,
            $"Updated location: {oldBuilding} {oldName} -> {location.Building} {location.Name}", isPublic: false);

        return true;
    }

    public async Task<bool> DeleteLocationAsync(int id, string actorUserId)
    {
        var location = await _db.Location
            .Include(l => l.FoundItem)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (location == null) return false;

        // Prevent deletion if has items
        if (location.FoundItem.Any())
            throw new ArgumentException("Không thể xoá địa điểm đang có đồ gắn vào");

        var locationName = location.Name;
        _db.Location.Remove(location);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Deleted", "Location", id.ToString(),
            null, null, $"Deleted location: {locationName}", isPublic: false);

        return true;
    }

    #endregion

    #region Tag Management

    public async Task<List<TagManagementViewModel>> GetAllTagsAsync()
    {
        var tags = await _db.Tag
            .Include(t => t.FoundItemTag)
            .OrderBy(t => t.DisplayTag)
            .ToListAsync();

        return tags.Select(t => new TagManagementViewModel
        {
            Id = t.Id,
            DisplayTag = t.DisplayTag,
            NormalizedTag = t.NormalizedTag,
            UsageCount = t.FoundItemTag.Count
        }).ToList();
    }

    public async Task<bool> MergeTagsAsync(int sourceTagId, int targetTagId, string actorUserId)
    {
        if (sourceTagId == targetTagId)
            throw new ArgumentException("Không thể gộp thẻ với chính nó");

        var sourceTag = await _db.Tag
            .Include(t => t.FoundItemTag)
            .FirstOrDefaultAsync(t => t.Id == sourceTagId);

        var targetTag = await _db.Tag.FindAsync(targetTagId);

        if (sourceTag == null || targetTag == null)
            throw new ArgumentException("One or both tags not found");

        // Migrate all FoundItemTag references
        var itemTags = await _db.FoundItemTag
            .Where(ft => ft.TagId == sourceTagId)
            .ToListAsync();

        foreach (var itemTag in itemTags)
        {
            // Check if the item already has the target tag
            var existing = await _db.FoundItemTag
                .FirstOrDefaultAsync(ft => ft.FoundItemId == itemTag.FoundItemId && ft.TagId == targetTagId);

            if (existing == null)
            {
                itemTag.TagId = targetTagId;
            }
            else
            {
                // Duplicate, remove the source reference
                _db.FoundItemTag.Remove(itemTag);
            }
        }

        var sourceTagName = sourceTag.DisplayTag;
        var targetTagName = targetTag.DisplayTag;

        _db.Tag.Remove(sourceTag);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Merged", "Tag", targetTagId.ToString(),
            null, null,
            $"Merged tag '{sourceTagName}' (ID: {sourceTagId}) into '{targetTagName}' (ID: {targetTagId})", isPublic: false);

        return true;
    }

    public async Task<bool> DeleteUnusedTagsAsync(string actorUserId)
    {
        var unusedTags = await _db.Tag
            .Include(t => t.FoundItemTag)
            .Where(t => !t.FoundItemTag.Any())
            .ToListAsync();

        if (!unusedTags.Any()) return false;

        var count = unusedTags.Count;
        _db.Tag.RemoveRange(unusedTags);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Deleted", "Tag", "bulk",
            null, null, $"Deleted {count} unused tags", isPublic: false);

        return true;
    }

    #endregion

    #region Unclaimed Items Management

    public async Task<List<UnclaimedItemViewModel>> GetUnclaimedItemsAsync()
    {
        var unclaimedStatus = (int)FoundItemStatus.Unclaimed;

        var items = await _db.FoundItem
            .Include(f => f.Category)
            .Include(f => f.Location)
            .Include(f => f.ReporterUser)
            .Where(f => f.Status == unclaimedStatus)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();

        return items.Select(item => new UnclaimedItemViewModel
        {
            Id = item.Id,
            Title = item.Title,
            CategoryName = item.Category.Name,
            LocationName = item.Location.Name,
            Status = (FoundItemStatus)item.Status,
            FoundAt = item.FoundAt,
            DaysUnclaimed = (int)(DateTime.UtcNow - item.CreatedAt).TotalDays,
            ReporterName = item.ReporterUser.FullName ?? item.ReporterUser.Email ?? "Unknown"
        }).ToList();
    }

    public async Task<bool> DisposeItemAsync(int itemId, string actorUserId)
    {
        var item = await _db.FoundItem.FindAsync(itemId);
        if (item == null) return false;

        var oldStatus = item.Status;
        item.Status = (int)FoundItemStatus.Disposed;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Disposed", "FoundItem", itemId.ToString(),
            oldStatus.ToString(), item.Status.ToString(),
            $"Item disposed: {item.Title}", isPublic: false);

        return true;
    }

    #endregion

    #region Post Management (moderating every user post: found + lost)

    public async Task<List<AdminPostViewModel>> GetAllPostsAsync()
    {
        var found = (await _db.FoundItem
                .Select(f => new
                {
                    f.Id,
                    f.Title,
                    Category = f.Category.Name,
                    Location = f.Location.Name,
                    Owner = f.ReporterUser.FullName ?? f.ReporterUser.Email,
                    f.Status,
                    Claims = f.Claim.Count,
                    f.CreatedAt
                })
                .ToListAsync())
            .Select(x => new AdminPostViewModel
            {
                Id = x.Id,
                Kind = ItemKind.Found,
                Title = x.Title,
                CategoryName = x.Category,
                LocationName = x.Location,
                OwnerName = x.Owner ?? "?",
                StatusText = FoundStatusText(x.Status),
                ClaimCount = x.Claims,
                CreatedAt = x.CreatedAt
            });

        var lostRaw = await _db.LostItem
            .Select(l => new
            {
                l.Id,
                l.Title,
                Category = l.Category.Name,
                Location = l.Location.Name,
                l.OwnerUserId,
                l.Status,
                l.CreatedAt
            })
            .ToListAsync();

        // LostItem has no user navigation (the FK is a bare string) — batch-resolve owner names.
        var ownerIds = lostRaw.Select(l => l.OwnerUserId).Distinct().ToList();
        var ownerNames = await _db.Users
            .Where(u => ownerIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName ?? u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var lost = lostRaw.Select(x => new AdminPostViewModel
        {
            Id = x.Id,
            Kind = ItemKind.Lost,
            Title = x.Title,
            CategoryName = x.Category,
            LocationName = x.Location,
            OwnerName = (ownerNames.TryGetValue(x.OwnerUserId, out var n) ? n : null) ?? "?",
            StatusText = LostStatusText(x.Status),
            ClaimCount = 0,
            CreatedAt = x.CreatedAt
        });

        return found.Concat(lost).OrderByDescending(p => p.CreatedAt).ToList();
    }

    public async Task<bool> DeletePostAsync(ItemKind kind, int id, string actorUserId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        if (kind == ItemKind.Found)
        {
            var meta = await _db.FoundItem.AsNoTracking()
                .Where(f => f.Id == id)
                .Select(f => new { f.ReporterUserId, f.Title })
                .FirstOrDefaultAsync();
            if (meta is null) return false;

            // FoundItem is blocked by a NO-ACTION parent (Claim); delete it first.
            // Deleting Claim cascades ClaimImage + ClaimMessage; deleting FoundItem cascades its images + tags.
            await _db.Claim.Where(c => c.FoundItemId == id).ExecuteDeleteAsync();
            await _db.FoundItem.Where(f => f.Id == id).ExecuteDeleteAsync();

            await _auditService.LogAsync(actorUserId, "Deleted", "FoundItem", id.ToString(),
                null, null, $"Admin gỡ bài đăng: {meta.Title}", isPublic: false);
            await _notificationService.PushAsync(meta.ReporterUserId, "PostRemoved",
                "Bài đăng đã bị gỡ", $"Bài \"{meta.Title}\" của bạn đã bị quản trị viên gỡ.", "/Items");
        }
        else
        {
            var meta = await _db.LostItem.AsNoTracking()
                .Where(l => l.Id == id)
                .Select(l => new { l.OwnerUserId, l.Title })
                .FirstOrDefaultAsync();
            if (meta is null) return false;

            // LostItem has no NO-ACTION children — deleting it cascades its images + tags at the DB level.
            await _db.LostItem.Where(l => l.Id == id).ExecuteDeleteAsync();

            await _auditService.LogAsync(actorUserId, "Deleted", "LostItem", id.ToString(),
                null, null, $"Admin gỡ bài đăng: {meta.Title}", isPublic: false);
            await _notificationService.PushAsync(meta.OwnerUserId, "PostRemoved",
                "Bài đăng đã bị gỡ", $"Bài \"{meta.Title}\" của bạn đã bị quản trị viên gỡ.", "/Items");
        }

        await tx.CommitAsync();
        return true;
    }

    private static string FoundStatusText(int status) => (FoundItemStatus)status switch
    {
        FoundItemStatus.PendingDropoff => "Chờ tiếp nhận",
        FoundItemStatus.Open => "Đang mở",
        FoundItemStatus.ClaimAccepted => "Đã duyệt nhận",
        FoundItemStatus.Returned => "Đã trả",
        FoundItemStatus.Unclaimed => "Chưa có người nhận",
        FoundItemStatus.Disposed => "Đã thanh lý",
        _ => "?"
    };

    private static string LostStatusText(int status) => (LostItemStatus)status switch
    {
        LostItemStatus.Open => "Đang tìm",
        LostItemStatus.Resolved => "Đã tìm thấy",
        LostItemStatus.Cancelled => "Đã huỷ",
        _ => "?"
    };

    #endregion

    #region Dashboard

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var totalItems = await _db.FoundItem.CountAsync();
        var returnedStatus = (int)FoundItemStatus.Returned;
        var returnedItems = await _db.FoundItem
            .Where(f => f.Status == returnedStatus)
            .CountAsync();

        var returnRate = totalItems > 0 ? (double)returnedItems / totalItems * 100 : 0;

        // Calculate average return time
        var returnedItemsWithData = await _db.FoundItem
            .Include(f => f.Claim)
            .Where(f => f.Status == returnedStatus)
            .Select(f => new
            {
                f.CreatedAt,
                HandledAt = f.Claim.FirstOrDefault(c => c.Status == (int)ClaimStatus.Accepted)!.HandledAt
            })
            .ToListAsync();

        double avgReturnDays = 0;
        if (returnedItemsWithData.Any())
        {
            var totalDays = returnedItemsWithData
                .Where(x => x.HandledAt.HasValue)
                .Sum(x => (x.HandledAt!.Value - x.CreatedAt).TotalDays);
            avgReturnDays = totalDays / returnedItemsWithData.Count;
        }

        // Longest unclaimed item
        var unclaimedStatus = (int)FoundItemStatus.Unclaimed;
        var longestUnclaimed = await _db.FoundItem
            .Where(f => f.Status == unclaimedStatus)
            .OrderBy(f => f.CreatedAt)
            .FirstOrDefaultAsync();

        var longestUnclaimedDays = longestUnclaimed != null
            ? (int)(DateTime.UtcNow - longestUnclaimed.CreatedAt).TotalDays
            : 0;

        // Top finders
        var topFinders = await _db.FoundItem
            .GroupBy(f => f.ReporterUserId)
            .Select(g => new TopFinderViewModel
            {
                UserId = g.Key,
                ItemsFound = g.Count()
            })
            .OrderByDescending(f => f.ItemsFound)
            .Take(5)
            .ToListAsync();

        // Fill in user names
        foreach (var finder in topFinders)
        {
            var user = await _db.Users.FindAsync(finder.UserId);
            finder.FullName = user?.FullName ?? user?.Email ?? "Unknown";
        }

        // Recent activities
        var recentActivities = await _db.AuditLog
            .Include(a => a.ActorUser)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new RecentActivityViewModel
            {
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                ActorName = a.ActorUser != null ? (a.ActorUser.FullName ?? a.ActorUser.Email ?? "Unknown") : "System",
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new DashboardViewModel
        {
            TotalFoundItems = totalItems,
            ReturnedItems = returnedItems,
            ReturnRate = Math.Round(returnRate, 2),
            AverageReturnDays = Math.Round(avgReturnDays, 2),
            LongestUnclaimedDays = longestUnclaimedDays,
            TopFinders = topFinders,
            RecentActivities = recentActivities
        };
    }

    #endregion

    #region Audit Log

    public async Task<List<AuditLogViewModel>> GetAuditLogsAsync(AuditLogFilterViewModel filter)
    {
        var query = _db.AuditLog
            .Include(a => a.ActorUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.ActorUserId))
        {
            query = query.Where(a => a.ActorUserId == filter.ActorUserId);
        }

        if (!string.IsNullOrEmpty(filter.EntityType))
        {
            query = query.Where(a => a.EntityType == filter.EntityType);
        }

        if (!string.IsNullOrEmpty(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= filter.ToDate.Value);
        }

        if (filter.IsPublic.HasValue)
        {
            query = query.Where(a => a.IsPublic == filter.IsPublic.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogViewModel
            {
                Id = a.Id,
                ActorUserId = a.ActorUserId,
                ActorName = a.ActorUser != null ? (a.ActorUser.FullName ?? a.ActorUser.Email ?? "Unknown") : "System",
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                FromStatus = a.FromStatus,
                ToStatus = a.ToStatus,
                Detail = a.Detail,
                IsPublic = a.IsPublic,
                CreatedAt = a.CreatedAt,
                IpAddress = a.IpAddress
            })
            .Take(100)
            .ToListAsync();
    }

    #endregion
}