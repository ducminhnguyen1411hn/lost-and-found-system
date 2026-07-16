namespace LostAndFound.Models.ViewModels.Admin;

public class TagManagementViewModel
{
    public int Id { get; set; }
    public string DisplayTag { get; set; } = string.Empty;
    public string NormalizedTag { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}