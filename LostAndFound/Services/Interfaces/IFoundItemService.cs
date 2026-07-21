using System.Security.Claims;
using LostAndFound.Models.ViewModels.FoundItems;

namespace LostAndFound.Services.Interfaces;

public interface IFoundItemService
{
    Task<int> CreateAsync(FoundItemCreateViewModel vm, string reporterUserId);

    Task<FoundItemDetailViewModel?> GetDetailAsync(int id, ClaimsPrincipal user);

    Task<FoundItemEditViewModel?> GetForEditAsync(int id, string userId);

    Task<bool> UpdateAsync(int id, FoundItemEditViewModel vm, string userId);

    Task<bool> DeleteAsync(int id, string userId);
}
