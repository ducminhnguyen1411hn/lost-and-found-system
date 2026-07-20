using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Camera;

namespace LostAndFound.Services.Interfaces;

/// <summary>Camera-check request channel (FR-CAM): a Member asks Staff to review footage of an area/time
/// for a lost item; Staff responds. Not wired to any real camera — just the request/response workflow.</summary>
public interface ICameraService
{
    /// <summary>An empty create form with the location dropdown filled.</summary>
    Task<CameraRequestCreateViewModel> GetCreateViewModelAsync();

    /// <summary>Creates a Pending request and notifies the Staff group. Returns the new id.</summary>
    Task<int> CreateAsync(CameraRequestCreateViewModel vm, string requesterUserId);

    /// <summary>The requester's own requests, newest first.</summary>
    Task<List<CameraRequestViewModel>> GetMineAsync(string userId);

    /// <summary>Every request (Staff queue), newest first, optionally filtered by status.</summary>
    Task<CameraListViewModel> GetAllAsync(CameraRequestStatus? status);

    /// <summary>Staff responds directly (one-step): <paramref name="outcome"/> must be Resolved or Rejected.
    /// Records the note + handler + time, audits, notifies the requester, in one transaction. Returns false
    /// if the request is missing, already closed, or the outcome is invalid.</summary>
    Task<bool> RespondAsync(int id, CameraRequestStatus outcome, string? note, string staffUserId);
}
