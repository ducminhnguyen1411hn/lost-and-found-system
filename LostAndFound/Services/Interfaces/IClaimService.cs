using System.Security.Claims;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Claims;
using LostAndFound.Models.ViewModels.Common;

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
    Task<PagedResult<MyClaimViewModel>> GetMyClaimsAsync(string userId, ClaimStatus? status, int page, int pageSize);

    /// <summary>The claim page for the claimant / item holder / Admin. Null for anyone else (404).</summary>
    Task<ClaimDetailViewModel?> GetClaimDetailAsync(int claimId, ClaimsPrincipal user);

    /// <summary>Post one message to the claim thread; notifies the counterparty. False if not allowed.</summary>
    Task<bool> PostMessageAsync(int claimId, string body, ClaimsPrincipal user);
}
