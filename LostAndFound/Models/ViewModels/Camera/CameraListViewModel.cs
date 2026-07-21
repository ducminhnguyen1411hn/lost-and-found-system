using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Camera;

public class CameraListViewModel
{
    public List<CameraRequestViewModel> Items { get; set; } = new();
    public CameraRequestStatus? StatusFilter { get; set; }
}
