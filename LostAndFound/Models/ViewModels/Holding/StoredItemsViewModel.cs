using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Holding;

/// <summary>The Staff "Kho đồ" page: the received-items list plus the current status filter (null = all).</summary>
public class StoredItemsViewModel
{
    public List<StoredItemViewModel> Items { get; set; } = new();
    public FoundItemStatus? StatusFilter { get; set; }
}
