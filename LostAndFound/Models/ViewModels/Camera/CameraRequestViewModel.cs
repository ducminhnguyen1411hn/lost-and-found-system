using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Camera;

public class CameraRequestViewModel
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public DateTime FromTime { get; set; }
    public DateTime ToTime { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public CameraRequestStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string? ResponseNote { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string? HandledByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? HandledAt { get; set; }
}
