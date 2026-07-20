namespace LostAndFound.Models.ViewModels.Holding;

/// <summary>One row on the Staff "Đồ chờ tiếp nhận" queue — a Custodial FoundItem sitting in
/// PendingDropoff, waiting for Staff to confirm physical receipt (FR-HOLD-02).</summary>
public class PendingIntakeViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    /// <summary>Stored UTC; the view renders it through AppTime.ToLocal.</summary>
    public DateTime FoundAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
