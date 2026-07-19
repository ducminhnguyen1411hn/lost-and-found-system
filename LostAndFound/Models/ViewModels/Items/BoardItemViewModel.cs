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

    /// <summary>The raw status int. Which enum it means depends on <see cref="Kind"/> — the two sides do
    /// NOT share a scale (FoundItemStatus.Open = 1 but LostItemStatus.Open = 0), so never compare it
    /// without checking Kind first.</summary>
    public int StatusRaw { get; init; }

    public bool IsOpen => Kind == ItemKind.Found
        ? StatusRaw == (int)FoundItemStatus.Open
        : StatusRaw == (int)LostItemStatus.Open;

    /// <summary>Label for the owner's own list — the public board only ever shows Open items.</summary>
    public string StatusText => Kind == ItemKind.Found
        ? (FoundItemStatus)StatusRaw switch
        {
            FoundItemStatus.PendingDropoff => "Chờ bàn giao cho Staff",
            FoundItemStatus.Open => "Đang mở",
            FoundItemStatus.ClaimAccepted => "Đã duyệt yêu cầu nhận",
            FoundItemStatus.Returned => "Đã trả",
            FoundItemStatus.Unclaimed => "Quá hạn không ai nhận",
            FoundItemStatus.Disposed => "Đã thanh lý",
            _ => StatusRaw.ToString()
        }
        : (LostItemStatus)StatusRaw switch
        {
            LostItemStatus.Open => "Đang tìm",
            LostItemStatus.Resolved => "Đã tìm thấy",
            LostItemStatus.Cancelled => "Đã huỷ",
            _ => StatusRaw.ToString()
        };
}
