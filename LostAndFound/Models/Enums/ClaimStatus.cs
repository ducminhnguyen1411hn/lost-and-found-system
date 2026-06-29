namespace LostAndFound.Models.Enums;

/// <summary>
/// Lifecycle of a <c>Claim</c>. Stored as <c>int</c> (column Claim.Status, CHECK 0..2).
/// LOCKED CONTRACT — do not rename or reorder.
/// </summary>
public enum ClaimStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2
}
