using System.Security.Claims;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.ViewComponents;

public class ItemClaimPanelViewComponent : ViewComponent
{
    private readonly IClaimService _claims;
    public ItemClaimPanelViewComponent(IClaimService claims) => _claims = claims;

    public async Task<IViewComponentResult> InvokeAsync(int itemId)
    {
        var vm = await _claims.GetItemClaimPanelAsync(itemId, (ClaimsPrincipal)User);
        return View(vm);
    }
}
