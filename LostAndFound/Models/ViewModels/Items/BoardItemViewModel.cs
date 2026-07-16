using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Items;

/// <summary>One card on the unified board. Carries NO PrivateMarks — blind listing.</summary>
public class BoardItemViewModel
{
    public int Id { get; init; }
    public ItemKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }   // FoundAt or LostAt (UTC)
    public DateTime CreatedAt { get; init; }
    public string? CoverImagePath { get; init; }
    public int ImageCount { get; init; }
    public IReadOnlyList<string> DisplayTags { get; init; } = Array.Empty<string>();
}
