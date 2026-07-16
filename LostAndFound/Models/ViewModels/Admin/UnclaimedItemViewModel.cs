using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Admin;

public class UnclaimedItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public FoundItemStatus Status { get; set; }
    public DateTime FoundAt { get; set; }
    public int DaysUnclaimed { get; set; }
    public string? ReporterName { get; set; }
}