using System.Security.Claims;
using LostAndFound.Models.ViewModels.Claims;

namespace LostAndFound.Services.Interfaces;

public interface IClaimService
{
    /// <summary>GET form data + guard. Returns null if the item can't be claimed by this user.</summary>
    Task<ClaimCreateViewModel?> GetCreateFormAsync(int foundItemId, ClaimsPrincipal user);

    /// <summary>Submit a claim. Throws InvalidOperationException (VN message) on a rule violation.</summary>
    Task<int> CreateAsync(ClaimCreateViewModel vm, ClaimsPrincipal user);

    Task<bool> AcceptAsync(int claimId, ClaimsPrincipal user);
    Task<bool> RejectAsync(int claimId, string rejectReason, ClaimsPrincipal user);
    Task<bool> ConfirmHandoverAsync(int foundItemId, ClaimsPrincipal user);   // holder side
    Task<bool> ConfirmReceivedAsync(int foundItemId, ClaimsPrincipal user);   // claimant side
    Task<bool> CancelAcceptanceAsync(int foundItemId, ClaimsPrincipal user);  // holder side

    Task<ItemClaimPanelViewModel> GetItemClaimPanelAsync(int foundItemId, ClaimsPrincipal user);
    Task<IReadOnlyList<MyClaimViewModel>> GetMyClaimsAsync(string userId);
}
