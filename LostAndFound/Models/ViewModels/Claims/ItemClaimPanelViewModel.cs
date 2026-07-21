using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Claims;

/// <summary>Everything the ItemClaimPanel ViewComponent needs, computed server-side per viewer.</summary>
public class ItemClaimPanelViewModel
{
    public int FoundItemId { get; init; }
    public FoundItemStatus Status { get; init; }

    /// <summary>Logged-in member, not holder/reporter, no active claim, item is Open.</summary>
    public bool CanClaim { get; init; }

    /// <summary>Viewer is holder/admin — the panel may show private claim data + accept/reject.</summary>
    public bool IsHolderView { get; init; }
    public IReadOnlyList<ClaimForHolderViewModel> PendingClaims { get; init; } = Array.Empty<ClaimForHolderViewModel>();
    public ClaimForHolderViewModel? AcceptedClaim { get; init; }

    /// <summary>Rejected claims (manual + auto-rejected on accept), holder/admin only — kept so a rejection
    /// stays traceable on the item page instead of vanishing. Read-only; no accept/reject actions.</summary>
    public IReadOnlyList<ClaimForHolderViewModel> RejectedClaims { get; init; } = Array.Empty<ClaimForHolderViewModel>();

    /// <summary>Viewer is the claimant whose claim was accepted — used to show them "you received this item"
    /// after return (their own name, so no privacy concern).</summary>
    public bool ViewerIsAcceptedClaimant { get; init; }

    /// <summary>The two-way handover card (item is ClaimAccepted and the viewer is one of the two
    /// parties). Null = don't render it. Shared with the claim page via the _HandoverPanel partial.</summary>
    public HandoverPanelViewModel? Handover { get; init; }
}
