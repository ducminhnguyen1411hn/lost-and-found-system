using System.Security.Claims;
using LostAndFound.Models.Enums;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Controllers;

/// <summary>Staff-side custodial intake queue (FR-HOLD-02): confirm receipt of dropped-off items so they
/// go public and become claimable. Custodial items are otherwise stuck in PendingDropoff forever.</summary>
[Authorize(Roles = "Staff,Admin")]
public class HoldingController : Controller
{
    private readonly IHoldingService _holding;

    public HoldingController(IHoldingService holding)
    {
        _holding = holding;
    }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET: /Holding
    public async Task<IActionResult> Index()
    {
        var model = await _holding.GetPendingIntakeAsync();
        return View(model);
    }

    // POST: /Holding/Confirm
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, string storageLocation)
    {
        if (string.IsNullOrWhiteSpace(storageLocation))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập nơi cất đồ trước khi tiếp nhận.";
            return RedirectToAction(nameof(Index));
        }

        var ok = await _holding.ConfirmReceiptAsync(id, storageLocation, Uid);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
            ? "Đã tiếp nhận đồ và mở để người mất nhận lại."
            : "Không tiếp nhận được — đồ không còn ở trạng thái chờ tiếp nhận.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Holding/Stored?status=
    public async Task<IActionResult> Stored(FoundItemStatus? status)
    {
        var model = await _holding.GetStoredAsync(status);
        return View(model);
    }

    // POST: /Holding/UpdateStorage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStorage(int id, string storageLocation, FoundItemStatus? status)
    {
        if (string.IsNullOrWhiteSpace(storageLocation))
        {
            TempData["ErrorMessage"] = "Nơi cất không được để trống.";
            return RedirectToAction(nameof(Stored), new { status });
        }

        var ok = await _holding.UpdateStorageLocationAsync(id, storageLocation, Uid);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
            ? "Đã cập nhật nơi cất."
            : "Không cập nhật được nơi cất.";
        return RedirectToAction(nameof(Stored), new { status });
    }
}
