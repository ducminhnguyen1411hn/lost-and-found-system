using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Holding;

namespace LostAndFound.Services.Interfaces;

public interface IHoldingService
{
    Task<List<PendingIntakeViewModel>> GetPendingIntakeAsync();

    Task<bool> ConfirmReceiptAsync(int itemId, string storageLocation, string staffUserId);

    Task<StoredItemsViewModel> GetStoredAsync(FoundItemStatus? status);

    Task<bool> UpdateStorageLocationAsync(int itemId, string storageLocation, string staffUserId);
}
