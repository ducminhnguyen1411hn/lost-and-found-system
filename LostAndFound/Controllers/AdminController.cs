using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.Admin;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LostAndFound.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IUnclaimedSweepService _sweep;

    public AdminController(IAdminService adminService, IUnclaimedSweepService sweep)
    {
        _adminService = adminService;
        _sweep = sweep;
    }

    #region Dashboard

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        var model = await _adminService.GetDashboardAsync();
        return View(model);
    }

    #endregion

    #region Category Management

    // GET: /Admin/Categories
    public async Task<IActionResult> Categories()
    {
        var model = await _adminService.GetAllCategoriesAsync();
        return View(model);
    }

    // GET: /Admin/Categories/Create
    public async Task<IActionResult> CreateCategory()
    {
        var model = await _adminService.GetCategoryCreateViewModelAsync();
        ViewBag.ParentCategories = await _adminService.GetAllCategoriesAsync();
        return View(model);
    }

    // POST: /Admin/Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ParentCategories = await _adminService.GetAllCategoriesAsync();
            return View(model);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var categoryId = await _adminService.CreateCategoryAsync(model, userId);
            TempData["SuccessMessage"] = "Category created successfully.";
            return RedirectToAction(nameof(Categories));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.ParentCategories = await _adminService.GetAllCategoriesAsync();
            return View(model);
        }
    }

    // GET: /Admin/Categories/Edit/{id}
    public async Task<IActionResult> EditCategory(int id)
    {
        var model = await _adminService.GetCategoryEditViewModelAsync(id);
        if (model == null) return NotFound();

        ViewBag.ParentCategories = await _adminService.GetAllCategoriesAsync();
        return View(model);
    }

    // POST: /Admin/Categories/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ParentCategories = await _adminService.GetAllCategoriesAsync();
            return View(model);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _adminService.UpdateCategoryAsync(model, userId);
            if (!result) return NotFound();

            TempData["SuccessMessage"] = "Category updated successfully.";
            return RedirectToAction(nameof(Categories));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.ParentCategories = await _adminService.GetAllCategoriesAsync();
            return View(model);
        }
    }

    // POST: /Admin/Categories/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _adminService.DeleteCategoryAsync(id, userId);
            if (!result) return NotFound();

            TempData["SuccessMessage"] = "Category deleted successfully.";
            return RedirectToAction(nameof(Categories));
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Categories));
        }
    }

    #endregion

    #region Location Management

    // GET: /Admin/Locations
    public async Task<IActionResult> Locations()
    {
        var model = await _adminService.GetAllLocationsAsync();
        return View(model);
    }

    // GET: /Admin/Locations/Create
    public async Task<IActionResult> CreateLocation()
    {
        var model = await _adminService.GetLocationCreateViewModelAsync();
        return View(model);
    }

    // POST: /Admin/Locations/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLocation(LocationCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var locationId = await _adminService.CreateLocationAsync(model, userId);
            TempData["SuccessMessage"] = "Location created successfully.";
            return RedirectToAction(nameof(Locations));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // GET: /Admin/Locations/Edit/{id}
    public async Task<IActionResult> EditLocation(int id)
    {
        var model = await _adminService.GetLocationEditViewModelAsync(id);
        if (model == null) return NotFound();

        return View(model);
    }

    // POST: /Admin/Locations/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLocation(LocationEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _adminService.UpdateLocationAsync(model, userId);
            if (!result) return NotFound();

            TempData["SuccessMessage"] = "Location updated successfully.";
            return RedirectToAction(nameof(Locations));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // POST: /Admin/Locations/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _adminService.DeleteLocationAsync(id, userId);
            if (!result) return NotFound();

            TempData["SuccessMessage"] = "Location deleted successfully.";
            return RedirectToAction(nameof(Locations));
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Locations));
        }
    }

    #endregion

    #region Tag Management

    // GET: /Admin/Tags
    public async Task<IActionResult> Tags()
    {
        var model = await _adminService.GetAllTagsAsync();
        return View(model);
    }

    // POST: /Admin/Tags/Merge
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MergeTags(int sourceTagId, int targetTagId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _adminService.MergeTagsAsync(sourceTagId, targetTagId, userId);
            if (!result) return NotFound();

            TempData["SuccessMessage"] = "Tags merged successfully.";
            return RedirectToAction(nameof(Tags));
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Tags));
        }
    }

    // POST: /Admin/Tags/Cleanup
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CleanupUnusedTags()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _adminService.DeleteUnusedTagsAsync(userId);

            if (result)
                TempData["SuccessMessage"] = "Unused tags cleaned up successfully.";
            else
                TempData["InfoMessage"] = "No unused tags found.";

            return RedirectToAction(nameof(Tags));
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Tags));
        }
    }

    #endregion

    #region Unclaimed Items

    // GET: /Admin/Unclaimed
    public async Task<IActionResult> Unclaimed()
    {
        ViewBag.OverdueDays = _sweep.OverdueDays;
        var model = await _adminService.GetUnclaimedItemsAsync();
        return View(model);
    }

    // POST: /Admin/Unclaimed/Sweep — mark overdue Open items (no pending claim) as Unclaimed.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SweepUnclaimed()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var marked = await _sweep.SweepOverdueAsync(userId);
        TempData[marked > 0 ? "SuccessMessage" : "InfoMessage"] = marked > 0
            ? $"Đã đánh dấu {marked} món quá hạn là chưa có người nhận."
            : $"Không có món nào quá {_sweep.OverdueDays} ngày mà chưa có người nhận.";
        return RedirectToAction(nameof(Unclaimed));
    }

    // POST: /Admin/Unclaimed/Dispose/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisposeItem(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _adminService.DisposeItemAsync(id, userId);
            if (!result) return NotFound();

            TempData["SuccessMessage"] = "Item disposed successfully.";
            return RedirectToAction(nameof(Unclaimed));
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Unclaimed));
        }
    }

    #endregion

    #region Post Management

    // GET: /Admin/Posts
    public async Task<IActionResult> Posts()
    {
        var model = await _adminService.GetAllPostsAsync();
        return View(model);
    }

    // POST: /Admin/Posts/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(ItemKind kind, int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var result = await _adminService.DeletePostAsync(kind, id, userId);
        if (!result)
        {
            TempData["ErrorMessage"] = "Không tìm thấy bài đăng (có thể đã bị xoá).";
            return RedirectToAction(nameof(Posts));
        }

        TempData["SuccessMessage"] = "Đã gỡ bài đăng và toàn bộ dữ liệu liên quan.";
        return RedirectToAction(nameof(Posts));
    }

    #endregion

    #region Audit Log

    // GET: /Admin/AuditLog
    public async Task<IActionResult> AuditLog([FromQuery] AuditLogFilterViewModel filter)
    {
        ViewBag.Filter = filter;
        var model = await _adminService.GetAuditLogsAsync(filter);
        return View(model);
    }

    #endregion
}