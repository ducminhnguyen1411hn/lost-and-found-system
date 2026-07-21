namespace LostAndFound.Models.ViewModels.Holding;

public class PendingIntakeViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public DateTime FoundAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
