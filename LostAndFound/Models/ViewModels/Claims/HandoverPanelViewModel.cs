namespace LostAndFound.Models.ViewModels.Claims;

public class HandoverPanelViewModel
{
    public int FoundItemId { get; init; }

    public string HolderName { get; init; } = string.Empty;
    public string ClaimantName { get; init; } = string.Empty;

    public bool HolderConfirmed { get; init; }
    public bool ClaimantConfirmed { get; init; }

    public DateTime? HolderConfirmedAt { get; init; }
    public DateTime? ClaimantConfirmedAt { get; init; }

    public bool ViewerIsHolder { get; init; }
    public bool ViewerIsClaimant { get; init; }

    public bool CanCancelAcceptance { get; init; }
}
