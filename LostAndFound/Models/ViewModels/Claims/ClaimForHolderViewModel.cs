using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Claims;

/// <summary>One claim as the item HOLDER (or Admin) sees it — includes private verification/evidence.
/// NEVER built for a non-holder viewer (blind listing).</summary>
public class ClaimForHolderViewModel
{
    public int ClaimId { get; init; }
    public string ClaimantName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string VerificationDetails { get; init; } = string.Empty;
    public IReadOnlyList<string> EvidenceImagePaths { get; init; } = Array.Empty<string>();
    public ClaimStatus Status { get; init; }
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    /// <summary>Why it was rejected (only set on rejected claims). Holder/admin only.</summary>
    public string? RejectReason { get; init; }
}
