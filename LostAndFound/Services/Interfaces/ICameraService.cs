using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Camera;

namespace LostAndFound.Services.Interfaces;

public interface ICameraService
{
    Task<CameraRequestCreateViewModel> GetCreateViewModelAsync();

    Task<int> CreateAsync(CameraRequestCreateViewModel vm, string requesterUserId);

    Task<List<CameraRequestViewModel>> GetMineAsync(string userId);

    Task<CameraListViewModel> GetAllAsync(CameraRequestStatus? status);

    Task<bool> RespondAsync(int id, CameraRequestStatus outcome, string? note, string staffUserId);
}
