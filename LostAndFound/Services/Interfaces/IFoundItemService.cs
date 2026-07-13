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
}
