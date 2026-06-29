namespace LostAndFound.Models.Enums;

/// <summary>
/// Where a found item is held. Determines who the "holder" is (the person who approves claims
/// and confirms handover): the reporter when <c>SelfHeld</c>, a staff member when <c>Custodial</c>.
/// Stored as <c>int</c> (column FoundItem.HoldingType, CHECK 0..1). LOCKED CONTRACT.
/// </summary>
public enum HoldingType
{
    SelfHeld = 0,  // default: finder keeps and returns the item directly
    Custodial = 1  // handed to staff to hold (typically valuable/sensitive items)
}
