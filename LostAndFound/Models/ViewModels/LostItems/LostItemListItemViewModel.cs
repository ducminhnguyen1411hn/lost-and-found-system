namespace LostAndFound.Models.ViewModels.LostItems;

/// <summary>One card on the public lost-item list.</summary>
public class LostItemListItemViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public DateTime LostAt { get; init; }
    public string? CoverImagePath { get; init; }
    public int ImageCount { get; init; }
    public IReadOnlyList<string> DisplayTags { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }

    /// <summary>Description trimmed to ~50 words for the card.</summary>
    public string? ShortDescription
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Description)) return null;
            var words = Description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            return words.Length <= 50 ? Description.Trim() : string.Join(' ', words.Take(50)) + "…";
        }
    }
}
