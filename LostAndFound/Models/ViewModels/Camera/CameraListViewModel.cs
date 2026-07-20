using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Camera;

/// <summary>The Staff camera queue: the request list plus the current status filter (null = all).</summary>
public class CameraListViewModel
{
    public List<CameraRequestViewModel> Items { get; set; } = new();
    public CameraRequestStatus? StatusFilter { get; set; }
}
