using System.Security.Claims;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Claims;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Controllers;

[Authorize(Roles = "Member,Staff,Admin")]
public class ClaimsController : Controller
{
    private readonly IClaimService _claims;
    public ClaimsController(IClaimService claims) => _claims = claims;

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IActionResult BackToItem(int itemId) => RedirectToAction("Details", "FoundItems", new { id = itemId });

    [HttpGet]
    public async Task<IActionResult> Create(int foundItemId)
    {
        var vm = await _claims.GetCreateFormAsync(foundItemId, User);
        if (vm is null)
        {
            TempData["SuccessMessage"] = "Không thể gửi yêu cầu nhận lại cho món đồ này.";
            return BackToItem(foundItemId);
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClaimCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            // Re-hydrate read-only context for redisplay.
            var reload = await _claims.GetCreateFormAsync(vm.FoundItemId, User);
            vm.ItemTitle = reload?.ItemTitle ?? vm.ItemTitle;
            vm.ItemCoverImage = reload?.ItemCoverImage;
            return View(vm);
        }
        try
        {
            await _claims.CreateAsync(vm, User);
            TempData["SuccessMessage"] = "Đã gửi yêu cầu nhận lại. Người giữ sẽ xem xét.";
            return BackToItem(vm.FoundItemId);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ImageUploadException)
        {
            // InvalidOperationException = a business-rule violation; ImageUploadException = a bad evidence
            // file (wrong type / too large / Cloudinary error). Both surface as a friendly ModelState error
            // rather than a 500 (upload runs before the transaction, so nothing is half-written).
            ModelState.AddModelError(string.Empty, ex.Message);
            var reload = await _claims.GetCreateFormAsync(vm.FoundItemId, User);
            vm.ItemTitle = reload?.ItemTitle ?? vm.ItemTitle;
            vm.ItemCoverImage = reload?.ItemCoverImage;
            return View(vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> My(MyClaimsSearchViewModel q)
    {
        q.Results = await _claims.GetMyClaimsAsync(Uid, q.Status, q.Page, q.PageSize);
        return View(q);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _claims.GetClaimDetailAsync(id, User);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostMessage(int id, string newMessage)
    {
        var ok = await _claims.PostMessageAsync(id, newMessage ?? string.Empty, User);
        if (!ok) TempData["SuccessMessage"] = "Không gửi được tin nhắn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int claimId, int foundItemId)
    {
        var ok = await _claims.AcceptAsync(claimId, User);
        TempData["SuccessMessage"] = ok ? "Đã chấp nhận yêu cầu nhận lại." : "Không thể chấp nhận yêu cầu này.";
        return BackToItem(foundItemId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectClaimViewModel vm, int foundItemId)
    {
        var ok = ModelState.IsValid && await _claims.RejectAsync(vm.ClaimId, vm.RejectReason, User);
        TempData["SuccessMessage"] = ok ? "Đã từ chối yêu cầu nhận lại." : "Không thể từ chối (thiếu lý do hoặc sai trạng thái).";
        return BackToItem(foundItemId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmHandover(int foundItemId)
    {
        var ok = await _claims.ConfirmHandoverAsync(foundItemId, User);
        TempData["SuccessMessage"] = ok ? "Đã xác nhận bàn giao." : "Không thể xác nhận.";
        return BackToItem(foundItemId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmReceived(int foundItemId)
    {
        var ok = await _claims.ConfirmReceivedAsync(foundItemId, User);
        TempData["SuccessMessage"] = ok ? "Đã xác nhận nhận đồ." : "Không thể xác nhận.";
        return BackToItem(foundItemId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAcceptance(int foundItemId)
    {
        var ok = await _claims.CancelAcceptanceAsync(foundItemId, User);
        TempData["SuccessMessage"] = ok ? "Đã huỷ chấp nhận, mở lại yêu cầu." : "Không thể huỷ.";
        return BackToItem(foundItemId);
    }
}
