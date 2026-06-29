namespace LostAndFound.Models.Enums;

/// <summary>
/// Lifecycle of a <c>FoundItem</c>. Stored as <c>int</c> in the DB (column FoundItem.Status,
/// guarded by CHECK 0..5). LOCKED CONTRACT — do not rename or reorder the members.
/// </summary>
public enum FoundItemStatus
{
    PendingDropoff = 0, // Custodial item awaiting staff intake
    Open = 1,           // public, claimable
    ClaimAccepted = 2,  // a claim was accepted, awaiting 2-way handover
    Returned = 3,       // handover confirmed by both sides (closed)
    Unclaimed = 4,      // expired without a successful claim
    Disposed = 5        // disposed / donated
}
