using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Holding;

/// <summary>One row on the Staff "Đồ đã tiếp nhận / Kho đồ" screen — a Custodial FoundItem that has been
/// received (CustodianStaffId set), so Staff can see what's in storage, where, and by whom (FR-HOLD-03).</summary>
public class StoredItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public FoundItemStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string? StorageLocation { get; set; }
    public string CustodianName { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    /// <summary>Stored UTC; the view renders it through AppTime.ToLocal.</summary>
    public DateTime CreatedAt { get; set; }
}
