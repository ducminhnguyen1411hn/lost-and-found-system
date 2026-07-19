namespace LostAndFound.Models.Enums;

/// <summary>Which side of the board an item is on. UI-level only — NOT a stored column
/// (found items live in FoundItem, lost items in LostItem).</summary>
public enum ItemKind
{
    Found = 0,
    Lost = 1
}
