using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.FoundItems;

/// <summary>Public found-item detail (FR-FOUND-03). PrivateMarks/StorageLocation are populated by the
/// service ONLY when the viewer is the holder/staff (blind listing); otherwise they stay null.</summary>
public class FoundItemDetailViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public DateTime FoundAt { get; init; }
    public string? ImagePath { get; init; }
    public string ReporterName { get; init; } = string.Empty;
    public HoldingType HoldingType { get; init; }
    public FoundItemStatus Status { get; init; }
    public IReadOnlyList<string> DisplayTags { get; init; } = Array.Empty<string>();

    public bool CanSeePrivate { get; init; }
    public string? PrivateMarks { get; init; }     // only set when CanSeePrivate
    public string? StorageLocation { get; init; }  // only set when CanSeePrivate

    /// <summary>True only for the owner when the item is still editable (Open/PendingDropoff, no claims).
    /// Drives the Sửa/Xoá buttons.</summary>
    public bool CanEdit { get; init; }

    public IReadOnlyList<PublicEvent> PublicEvents { get; init; } = Array.Empty<PublicEvent>();

    /// <summary>A public (IsPublic=1) AuditLog row — the seed of the FR-TL timeline.</summary>
    public class PublicEvent
    {
        public DateTime At { get; init; }
        public string Action { get; init; } = string.Empty;
        public string? ToStatus { get; init; }
        public string? Detail { get; init; }
    }
}
