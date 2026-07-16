using LostAndFound.Models.Entities;
using LostAndFound.Models.ViewModels.Admin;

namespace LostAndFound.Services.Interfaces;

public interface IAdminService
{
    // Category Management
    Task<List<CategoryViewModel>> GetAllCategoriesAsync();
    Task<CategoryViewModel?> GetCategoryByIdAsync(int id);
    Task<CategoryCreateViewModel> GetCategoryCreateViewModelAsync();
    Task<CategoryEditViewModel?> GetCategoryEditViewModelAsync(int id);
    Task<int> CreateCategoryAsync(CategoryCreateViewModel model, string actorUserId);
    Task<bool> UpdateCategoryAsync(CategoryEditViewModel model, string actorUserId);
    Task<bool> DeleteCategoryAsync(int id, string actorUserId);

    // Location Management
    Task<List<LocationViewModel>> GetAllLocationsAsync();
    Task<LocationViewModel?> GetLocationByIdAsync(int id);
    Task<LocationCreateViewModel> GetLocationCreateViewModelAsync();
    Task<LocationEditViewModel?> GetLocationEditViewModelAsync(int id);
    Task<int> CreateLocationAsync(LocationCreateViewModel model, string actorUserId);
    Task<bool> UpdateLocationAsync(LocationEditViewModel model, string actorUserId);
    Task<bool> DeleteLocationAsync(int id, string actorUserId);

    // Tag Management
    Task<List<TagManagementViewModel>> GetAllTagsAsync();
    Task<bool> MergeTagsAsync(int sourceTagId, int targetTagId, string actorUserId);
    Task<bool> DeleteUnusedTagsAsync(string actorUserId);

    // Unclaimed Items Management
    Task<List<UnclaimedItemViewModel>> GetUnclaimedItemsAsync();
    Task<bool> DisposeItemAsync(int itemId, string actorUserId);

    // Dashboard
    Task<DashboardViewModel> GetDashboardAsync();

    // Audit Log
    Task<List<AuditLogViewModel>> GetAuditLogsAsync(AuditLogFilterViewModel filter);
}