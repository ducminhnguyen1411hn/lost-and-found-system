using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Common;

namespace LostAndFound.Models.ViewModels.Claims;

public class MyClaimsSearchViewModel
{
    public ClaimStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public PagedResult<MyClaimViewModel> Results { get; set; } = new();
}
