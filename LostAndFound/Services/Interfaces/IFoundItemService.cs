using System.Security.Claims;
using LostAndFound.Models.ViewModels.Common;
using LostAndFound.Models.ViewModels.FoundItems;

namespace LostAndFound.Services.Interfaces;

/// <summary>Found-item report + public lookup (FR-FOUND). Owns the create rule (state + tags + audit
/// in one transaction) and enforces blind-listing when projecting to view models.</summary>
public interface IFoundItemService
{
    /// <summary>Creates a FoundItem (SelfHeld → Open, Custodial → PendingDropoff), resolves tags,
    /// writes one AuditLog — all in a single transaction. Returns the new item id.</summary>
    Task<int> CreateAsync(FoundItemCreateViewModel vm, string reporterUserId);

    /// <summary>Public list: only <c>Open</c> items, LIKE + Category/Location/Tag filters, paginated.</summary>
    Task<PagedResult<FoundItemListItemViewModel>> SearchAsync(FoundItemSearchViewModel query);

    /// <summary>Public detail. Returns null when missing or not visible to <paramref name="user"/>.
    /// PrivateMarks/StorageLocation are filled only for the holder/staff.</summary>
    Task<FoundItemDetailViewModel?> GetDetailAsync(int id, ClaimsPrincipal user);

    /// <summary>Loads an item for the edit form. Returns null if missing, not owned by
    /// <paramref name="userId"/>, or no longer editable (not Open/PendingDropoff, or a claim exists).</summary>
    Task<FoundItemEditViewModel?> GetForEditAsync(int id, string userId);

    /// <summary>Applies an edit (owner-only, editable-only) + one AuditLog in a transaction.
    /// Returns false if not allowed. Throws <see cref="Images.ImageUploadException"/> on a bad new image.</summary>
    Task<bool> UpdateAsync(int id, FoundItemEditViewModel vm, string userId);

    /// <summary>Deletes an item (owner-only, editable-only) + one AuditLog. Returns false if not allowed.</summary>
    Task<bool> DeleteAsync(int id, string userId);
}
