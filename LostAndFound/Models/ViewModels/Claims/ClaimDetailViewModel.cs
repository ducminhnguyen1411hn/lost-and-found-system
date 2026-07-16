using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Claims;

/// <summary>The one page both parties use for a claim. Only ever built for the claimant, the item's
/// holder, or an Admin — so carrying VerificationDetails/evidence/messages here is safe.</summary>
public class ClaimDetailViewModel
{
    public int ClaimId { get; init; }
    public int FoundItemId { get; init; }
    public string ItemTitle { get; init; } = string.Empty;
    public string? ItemCoverImage { get; init; }
    public FoundItemStatus ItemStatus { get; init; }

    public string ClaimantName { get; init; } = string.Empty;
    public ClaimStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RejectReason { get; init; }
    public string VerificationDetails { get; init; } = string.Empty;
    public IReadOnlyList<string> EvidenceImagePaths { get; init; } = Array.Empty<string>();
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }

    public IReadOnlyList<ClaimMessageViewModel> Messages { get; init; } = Array.Empty<ClaimMessageViewModel>();

    public bool ViewerIsHolder { get; init; }
    /// <summary>Claim is still live (Pending/Accepted). A Rejected claim's thread is read-only.</summary>
    public bool CanPostMessage { get; init; }
    public bool CanAccept { get; init; }
    public bool CanReject { get; init; }

    /// <summary>The two-way handover card, or null when it must not render. Same partial as the item page.</summary>
    public HandoverPanelViewModel? Handover { get; init; }

    // bound on the post-message form
    [Required(ErrorMessage = "Nhập nội dung tin nhắn.")]
    [StringLength(2000)]
    [Display(Name = "Nhắn cho bên kia")]
    public string? NewMessage { get; set; }
}
