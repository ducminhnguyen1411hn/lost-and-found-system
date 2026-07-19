using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Admin;

/// <summary>One row on the admin "Bài đăng" moderation list. Spans both sides of the board
/// (FoundItem + LostItem), so <see cref="Kind"/> disambiguates which table the <see cref="Id"/> is in.</summary>
public class AdminPostViewModel
{
    public int Id { get; set; }
    public ItemKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    /// <summary>Number of claims on the post (FoundItem only; always 0 for lost). A post with claims
    /// is riskier to delete — the view warns before cascading them away.</summary>
    public int ClaimCount { get; set; }
    /// <summary>Stored UTC; the view renders it through AppTime.ToLocal.</summary>
    public DateTime CreatedAt { get; set; }
}
