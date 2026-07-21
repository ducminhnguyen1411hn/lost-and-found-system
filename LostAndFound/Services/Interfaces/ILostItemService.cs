using System.Security.Claims;
using LostAndFound.Models.ViewModels.LostItems;

namespace LostAndFound.Services.Interfaces;

public interface ILostItemService
{
    Task<int> CreateAsync(LostItemCreateViewModel vm, string ownerUserId);
    Task<LostItemDetailViewModel?> GetDetailAsync(int id, ClaimsPrincipal user);
    Task<LostItemEditViewModel?> GetForEditAsync(int id, string userId);
    Task<bool> UpdateAsync(int id, LostItemEditViewModel vm, string userId);
    Task<bool> DeleteAsync(int id, string userId);

    Task<bool> MarkResolvedAsync(int id, string userId);
}
