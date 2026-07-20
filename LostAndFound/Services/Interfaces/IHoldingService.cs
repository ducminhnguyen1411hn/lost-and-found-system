using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Holding;

namespace LostAndFound.Services.Interfaces;

/// <summary>Staff-side custodial intake (FR-HOLD-02) + stored-items management (FR-HOLD-03). A Custodial post
/// is created in PendingDropoff and stays hidden until Staff physically receives the item and opens it.</summary>
public interface IHoldingService
{
    /// <summary>Custodial items awaiting Staff receipt (Status = PendingDropoff), oldest first.</summary>
    Task<List<PendingIntakeViewModel>> GetPendingIntakeAsync();

    /// <summary>Staff confirms physical receipt: records the storage location, becomes the custodian/holder,
    /// and moves the item PendingDropoff → Open. Writes an audit row and notifies the reporter, in one
    /// transaction. Returns false if the item is missing or no longer awaiting intake.</summary>
    Task<bool> ConfirmReceiptAsync(int itemId, string storageLocation, string staffUserId);

    /// <summary>Custodial items that have already been received (CustodianStaffId set), newest first,
    /// optionally filtered by status. This is the storage inventory (FR-HOLD-03).</summary>
    Task<StoredItemsViewModel> GetStoredAsync(FoundItemStatus? status);

    /// <summary>Staff edits where a received item is stored. Audited (internal, non-public). Returns false
    /// if the item is missing or was never received into custody.</summary>
    Task<bool> UpdateStorageLocationAsync(int itemId, string storageLocation, string staffUserId);
}
