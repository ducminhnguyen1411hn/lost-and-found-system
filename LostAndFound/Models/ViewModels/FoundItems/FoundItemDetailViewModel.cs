using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.FoundItems;

public class FoundItemDetailViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public DateTime FoundAt { get; init; }
    public IReadOnlyList<string> ImagePaths { get; init; } = Array.Empty<string>();
    public string ReporterName { get; init; } = string.Empty;
    public HoldingType HoldingType { get; init; }
    public FoundItemStatus Status { get; init; }
    public IReadOnlyList<string> DisplayTags { get; init; } = Array.Empty<string>();

    public bool CanSeePrivate { get; init; }
    public string? PrivateMarks { get; init; }
    public string? StorageLocation { get; init; }

    public bool CanEdit { get; init; }

    public IReadOnlyList<PublicEvent> PublicEvents { get; init; } = Array.Empty<PublicEvent>();

    public class PublicEvent
    {
        public DateTime At { get; init; }
        public string Action { get; init; } = string.Empty;
        public string? ToStatus { get; init; }
        public string? Detail { get; init; }
        public string? ActorName { get; init; }
    }
}
