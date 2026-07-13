namespace LostAndFound.Models.ViewModels.FoundItems;

/// <summary>One card in the public found-item list (FR-FOUND-03/04).
/// Deliberately carries NO PrivateMarks — blind listing.</summary>
public class FoundItemListItemViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public DateTime FoundAt { get; init; }
    public string? CoverImagePath { get; init; } // the cover (lowest SortOrder), or null
    public int ImageCount { get; init; }          // total images (drives the "+N" badge)
    public IReadOnlyList<string> DisplayTags { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }

    /// <summary>Description trimmed to ~50 words for the card (adds an ellipsis when cut).</summary>
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
