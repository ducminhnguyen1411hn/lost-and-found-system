namespace LostAndFound.Models.Enums;

/// <summary>
/// Lifecycle of a <c>LostItem</c> (the public "I lost this" post). Stored as <c>int</c>
/// (column LostItem.Status, guarded by CHECK 0..2).
/// </summary>
public enum LostItemStatus
{
    Open = 0,      // still looking, shown publicly
    Resolved = 1,  // owner found/recovered the item
    Cancelled = 2  // owner gave up / withdrew the post
}
