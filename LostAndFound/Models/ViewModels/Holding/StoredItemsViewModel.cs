using LostAndFound.Models.Enums;

namespace LostAndFound.Models.ViewModels.Holding;

public class StoredItemsViewModel
{
    public List<StoredItemViewModel> Items { get; set; } = new();
    public FoundItemStatus? StatusFilter { get; set; }
}
