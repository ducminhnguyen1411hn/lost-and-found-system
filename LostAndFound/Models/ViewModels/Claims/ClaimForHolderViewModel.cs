using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Claims;

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
    public string? RejectReason { get; init; }
}
