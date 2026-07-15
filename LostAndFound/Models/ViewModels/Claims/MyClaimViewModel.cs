using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Claims;

/// <summary>The claimant's view of their OWN claim.</summary>
public class MyClaimViewModel
{
    public int ClaimId { get; init; }
    public int FoundItemId { get; init; }
    public string ItemTitle { get; init; } = string.Empty;
    public string? ItemCoverImage { get; init; }
    public ClaimStatus Status { get; init; }
    public FoundItemStatus ItemStatus { get; init; }
    public string? RejectReason { get; init; }
    public DateTime CreatedAt { get; init; }
    /// <summary>Item is ClaimAccepted, this claim is the Accepted one, and the claimant has not yet confirmed receipt.</summary>
    public bool CanConfirmReceived { get; init; }
    public bool HolderConfirmed { get; init; }
    public bool ClaimantConfirmed { get; init; }
}
