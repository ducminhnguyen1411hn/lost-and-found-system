using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Holding;

public class StoredItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public FoundItemStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string? StorageLocation { get; set; }
    public string CustodianName { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
