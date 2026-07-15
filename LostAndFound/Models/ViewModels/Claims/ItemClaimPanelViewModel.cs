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

    // Handover (item is ClaimAccepted)
    public bool HolderConfirmed { get; init; }
    public bool ClaimantConfirmed { get; init; }
    public bool ShowHolderHandover { get; init; }    // viewer is holder
    public bool ShowClaimantHandover { get; init; }  // viewer is the accepted claimant
    public bool CanCancelAcceptance { get; init; }   // viewer is holder
}
