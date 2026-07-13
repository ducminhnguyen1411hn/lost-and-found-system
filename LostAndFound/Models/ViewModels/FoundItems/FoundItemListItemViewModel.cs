namespace LostAndFound.Models.ViewModels.FoundItems;

/// <summary>One card in the public found-item list (FR-FOUND-03/04).
/// Deliberately carries NO PrivateMarks — blind listing.</summary>
public class FoundItemListItemViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public DateTime FoundAt { get; init; }
    public string? ImagePath { get; init; }
    public IReadOnlyList<string> DisplayTags { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
}
