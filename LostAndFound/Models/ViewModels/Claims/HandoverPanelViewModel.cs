namespace LostAndFound.Models.ViewModels.Claims;

/// <summary>
/// The two-way handover card, shared by the item page (ItemClaimPanel) and the claim page so the two
/// can't drift apart. Only ever built when the viewer is the holder or the accepted claimant, so the
/// names carried here are safe to render — never build it for anyone else.
/// </summary>
public class HandoverPanelViewModel
{
    public int FoundItemId { get; init; }

    public string HolderName { get; init; } = string.Empty;
    public string ClaimantName { get; init; } = string.Empty;

    public bool HolderConfirmed { get; init; }
    public bool ClaimantConfirmed { get; init; }

    /// <summary>When each side confirmed — stored UTC, render through AppTime. Null while still waiting.</summary>
    public DateTime? HolderConfirmedAt { get; init; }
    public DateTime? ClaimantConfirmedAt { get; init; }

    /// <summary>Viewer is the holder — show their confirm button + the cancel action.</summary>
    public bool ViewerIsHolder { get; init; }
    /// <summary>Viewer is the accepted claimant — show their confirm button.</summary>
    public bool ViewerIsClaimant { get; init; }

    public bool CanCancelAcceptance { get; init; }
}
