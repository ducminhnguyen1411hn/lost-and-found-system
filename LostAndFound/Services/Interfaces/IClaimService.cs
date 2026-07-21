using System.Security.Claims;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Claims;
using LostAndFound.Models.ViewModels.Common;

namespace LostAndFound.Services.Interfaces;

public interface IClaimService
{
    Task<ClaimCreateViewModel?> GetCreateFormAsync(int foundItemId, ClaimsPrincipal user);

    Task<int> CreateAsync(ClaimCreateViewModel vm, ClaimsPrincipal user);

    Task<bool> AcceptAsync(int claimId, ClaimsPrincipal user);
    Task<bool> RejectAsync(int claimId, string rejectReason, ClaimsPrincipal user);
    Task<bool> ConfirmHandoverAsync(int foundItemId, ClaimsPrincipal user);
    Task<bool> ConfirmReceivedAsync(int foundItemId, ClaimsPrincipal user);
    Task<bool> CancelAcceptanceAsync(int foundItemId, ClaimsPrincipal user);

    Task<ItemClaimPanelViewModel> GetItemClaimPanelAsync(int foundItemId, ClaimsPrincipal user);
    Task<PagedResult<MyClaimViewModel>> GetMyClaimsAsync(string userId, ClaimStatus? status, int page, int pageSize);

    Task<ClaimDetailViewModel?> GetClaimDetailAsync(int claimId, ClaimsPrincipal user);

    Task<bool> PostMessageAsync(int claimId, string body, ClaimsPrincipal user);
}
