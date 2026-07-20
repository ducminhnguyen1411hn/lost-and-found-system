using System.Security.Claims;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Camera;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Controllers;

/// <summary>Camera-check requests (FR-CAM). Members create/track requests; Staff (+Admin) triage the queue
/// and respond. One-step flow: Staff answers Pending directly with Resolved / Rejected + a note.</summary>
[Authorize]
public class CameraController : Controller
{
    private readonly ICameraService _camera;

    public CameraController(ICameraService camera)
    {
        _camera = camera;
    }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET: /Camera/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = await _camera.GetCreateViewModelAsync();
        return View(vm);
    }

    // POST: /Camera/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CameraRequestCreateViewModel vm)
    {
        if (vm.FromTime.HasValue && vm.ToTime.HasValue && vm.FromTime >= vm.ToTime)
            ModelState.AddModelError(nameof(vm.ToTime), "Thời điểm kết thúc phải sau thời điểm bắt đầu.");

        if (!ModelState.IsValid)
        {
            vm.Locations = (await _camera.GetCreateViewModelAsync()).Locations;
            return View(vm);
        }

        await _camera.CreateAsync(vm, Uid);
        TempData["SuccessMessage"] = "Đã gửi yêu cầu trích camera. Nhân viên sẽ phản hồi sớm.";
        return RedirectToAction(nameof(Mine));
    }

    // GET: /Camera/Mine
    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var model = await _camera.GetMineAsync(Uid);
        return View(model);
    }

    // GET: /Camera  — Staff queue
    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Index(CameraRequestStatus? status)
    {
        var model = await _camera.GetAllAsync(status);
        return View(model);
    }

    // POST: /Camera/Respond
    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(int id, CameraRequestStatus outcome, string? note, CameraRequestStatus? status)
    {
        var ok = await _camera.RespondAsync(id, outcome, note, Uid);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
            ? "Đã phản hồi yêu cầu."
            : "Không phản hồi được — yêu cầu đã đóng hoặc không hợp lệ.";
        return RedirectToAction(nameof(Index), new { status });
    }
}
