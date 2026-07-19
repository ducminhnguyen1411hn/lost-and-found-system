using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.LostItems;

/// <summary>Public lost-item detail.</summary>
public class LostItemDetailViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public DateTime LostAt { get; init; }
    public IReadOnlyList<string> ImagePaths { get; init; } = Array.Empty<string>();
    public string OwnerName { get; init; } = string.Empty;
    public LostItemStatus Status { get; init; }
    public IReadOnlyList<string> DisplayTags { get; init; } = Array.Empty<string>();

    /// <summary>True only for the owner while the post is still Open — drives Sửa/Xoá/Đã tìm thấy.</summary>
    public bool CanEdit { get; init; }

    public IReadOnlyList<PublicEvent> PublicEvents { get; init; } = Array.Empty<PublicEvent>();

    public class PublicEvent
    {
        public DateTime At { get; init; }
        public string Action { get; init; } = string.Empty;
        public string? Detail { get; init; }
        public string? ActorName { get; init; }
    }
}
