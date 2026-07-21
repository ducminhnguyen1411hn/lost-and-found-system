using System.ComponentModel.DataAnnotations;
using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Claims;

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
    public bool CanPostMessage { get; init; }
    public bool CanAccept { get; init; }
    public bool CanReject { get; init; }

    public HandoverPanelViewModel? Handover { get; init; }

    [Required(ErrorMessage = "Nhập nội dung tin nhắn.")]
    [StringLength(2000)]
    [Display(Name = "Nhắn cho bên kia")]
    public string? NewMessage { get; set; }
}
