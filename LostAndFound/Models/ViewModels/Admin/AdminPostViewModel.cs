using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Admin;

public class AdminPostViewModel
{
    public int Id { get; set; }
    public ItemKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public int ClaimCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
