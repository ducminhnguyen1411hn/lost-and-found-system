using LostAndFound.Models.Entities;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Admin;

namespace LostAndFound.Services.Interfaces;

public interface IAdminService
{

    Task<List<AdminPostViewModel>> GetAllPostsAsync();
    Task<bool> DeletePostAsync(ItemKind kind, int id, string actorUserId);

    Task<List<CategoryViewModel>> GetAllCategoriesAsync();
    Task<CategoryCreateViewModel> GetCategoryCreateViewModelAsync();
    Task<CategoryEditViewModel?> GetCategoryEditViewModelAsync(int id);
    Task<int> CreateCategoryAsync(CategoryCreateViewModel model, string actorUserId);
    Task<bool> UpdateCategoryAsync(CategoryEditViewModel model, string actorUserId);
    Task<bool> DeleteCategoryAsync(int id, string actorUserId);

    Task<List<LocationViewModel>> GetAllLocationsAsync();
    Task<LocationCreateViewModel> GetLocationCreateViewModelAsync();
    Task<LocationEditViewModel?> GetLocationEditViewModelAsync(int id);
    Task<int> CreateLocationAsync(LocationCreateViewModel model, string actorUserId);
    Task<bool> UpdateLocationAsync(LocationEditViewModel model, string actorUserId);
    Task<bool> DeleteLocationAsync(int id, string actorUserId);

    Task<List<TagManagementViewModel>> GetAllTagsAsync();
    Task<bool> MergeTagsAsync(int sourceTagId, int targetTagId, string actorUserId);
    Task<bool> DeleteUnusedTagsAsync(string actorUserId);

    Task<List<UnclaimedItemViewModel>> GetUnclaimedItemsAsync();
    Task<bool> DisposeItemAsync(int itemId, string actorUserId);

    Task<DashboardViewModel> GetDashboardAsync();

    Task<List<AuditLogViewModel>> GetAuditLogsAsync(AuditLogFilterViewModel filter);
}