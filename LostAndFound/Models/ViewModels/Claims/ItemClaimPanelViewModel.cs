using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Claims;

public class ItemClaimPanelViewModel
{
    public int FoundItemId { get; init; }
    public FoundItemStatus Status { get; init; }

    public bool CanClaim { get; init; }

    public bool IsHolderView { get; init; }
    public IReadOnlyList<ClaimForHolderViewModel> PendingClaims { get; init; } = Array.Empty<ClaimForHolderViewModel>();
    public ClaimForHolderViewModel? AcceptedClaim { get; init; }

    public IReadOnlyList<ClaimForHolderViewModel> RejectedClaims { get; init; } = Array.Empty<ClaimForHolderViewModel>();

    public bool ViewerIsAcceptedClaimant { get; init; }

    public HandoverPanelViewModel? Handover { get; init; }
}
