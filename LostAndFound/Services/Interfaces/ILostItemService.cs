using System.Security.Claims;
using LostAndFound.Models.ViewModels.LostItems;

namespace LostAndFound.Services.Interfaces;

/// <summary>Public "lost item" board — the active counterpart to found items. Owner posts what they lost;
/// others browse it. Mirrors <see cref="IFoundItemService"/> minus holding/verification concerns.</summary>
public interface ILostItemService
{
    Task<int> CreateAsync(LostItemCreateViewModel vm, string ownerUserId);
    Task<LostItemDetailViewModel?> GetDetailAsync(int id, ClaimsPrincipal user);
    Task<LostItemEditViewModel?> GetForEditAsync(int id, string userId);
    Task<bool> UpdateAsync(int id, LostItemEditViewModel vm, string userId);
    Task<bool> DeleteAsync(int id, string userId);

    /// <summary>Owner marks the post Resolved (item recovered). Returns false if not allowed.</summary>
    Task<bool> MarkResolvedAsync(int id, string userId);
}
