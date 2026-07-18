using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models.ViewModels.FoundItems;
using LostAndFound.Services;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Controllers;

/// <summary>Thin controller for FR-FOUND. All rules live in <see cref="IFoundItemService"/>;
/// this only orchestrates: bind → validate → call service → view/redirect.</summary>
public class FoundItemsController : Controller
{
    private readonly IFoundItemService _service;
    private readonly ApplicationDbContext _db;

    public FoundItemsController(IFoundItemService service, ApplicationDbContext db)
    {
        _service = service;
        _db = db;
    }

    // GET /FoundItems — public list + search/filter/pagination
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(FoundItemSearchViewModel q)
    {
        q.Categories = await BuildCategorySelectAsync(q.CategoryId);
        q.Locations = await BuildLocationSelectAsync(q.LocationId);
        q.Results = await _service.SearchAsync(q);
        return View(q);
    }

    // GET /FoundItems/Details/5 — public detail (service gates hidden fields)
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _service.GetDetailAsync(id, User);
        if (vm is null) return NotFound();
        return View(vm);
    }

    // GET /FoundItems/Create — report form
    [Authorize(Roles = "Member,Staff")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        // Check if user is posting blocked
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _db.Users.FindAsync(userId);
        if (user != null && user.IsPostingBlocked)
        {
            TempData["ErrorMessage"] = "Bạn đã bị chặn đăng bài. Vui lòng liên hệ quản trị viên.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new FoundItemCreateViewModel
        {
            FoundAt = AppTime.LocalNow,
            Categories = await BuildCategorySelectAsync(),
            Locations = await BuildLocationSelectAsync()
        };
        return View(vm);
    }

    // POST /FoundItems/Create
    [Authorize(Roles = "Member,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FoundItemCreateViewModel vm)
    {
        // Check if user is posting blocked
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _db.Users.FindAsync(userId);
        if (user != null && user.IsPostingBlocked)
        {
            TempData["ErrorMessage"] = "Bạn đã bị chặn đăng bài. Vui lòng liên hệ quản trị viên.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return await RedisplayCreate(vm);

        try
        {
            var id = await _service.CreateAsync(vm, userId);
            TempData["SuccessMessage"] = "Đã đăng đồ nhặt được thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ImageUploadException ex)
        {
            ModelState.AddModelError(nameof(FoundItemCreateViewModel.CoverImage), ex.Message);
            return await RedisplayCreate(vm);
        }
    }

    private async Task<IActionResult> RedisplayCreate(FoundItemCreateViewModel vm)
    {
        vm.Categories = await BuildCategorySelectAsync(vm.CategoryId);
        vm.Locations = await BuildLocationSelectAsync(vm.LocationId);
        return View(vm);
    }

    // GET /FoundItems/Edit/5 — owner-only (service enforces)
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var vm = await _service.GetForEditAsync(id, userId);
        if (vm is null) return NotFound();
        vm.Categories = await BuildCategorySelectAsync(vm.CategoryId);
        vm.Locations = await BuildLocationSelectAsync(vm.LocationId);
        return View(vm);
    }

    // POST /FoundItems/Edit/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FoundItemEditViewModel vm)
    {
        vm.Id = id;
        if (!ModelState.IsValid)
            return await RedisplayEdit(vm);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var ok = await _service.UpdateAsync(id, vm, userId);
            if (!ok) return NotFound();
            TempData["SuccessMessage"] = "Đã cập nhật bài đăng.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ImageUploadException ex)
        {
            ModelState.AddModelError(nameof(FoundItemEditViewModel.NewImages), ex.Message);
            return await RedisplayEdit(vm);
        }
    }

    // POST /FoundItems/Delete/5 — owner-only (service enforces)
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var ok = await _service.DeleteAsync(id, userId);
        if (!ok) return NotFound();
        TempData["SuccessMessage"] = "Đã xoá bài đăng.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RedisplayEdit(FoundItemEditViewModel vm)
    {
        vm.Categories = await BuildCategorySelectAsync(vm.CategoryId);
        vm.Locations = await BuildLocationSelectAsync(vm.LocationId);
        vm.ExistingImages = await _db.FoundItemImage.AsNoTracking()
            .Where(im => im.FoundItemId == vm.Id)
            .OrderBy(im => im.SortOrder)
            .Select(im => new FoundItemEditViewModel.ImageItem { Id = im.Id, Url = im.Url })
            .ToListAsync();
        return View(vm);
    }

    private async Task<List<SelectListItem>> BuildCategorySelectAsync(int? selected = null)
    {
        var cats = await _db.Category.AsNoTracking().ToListAsync();
        var parents = cats.Where(c => c.ParentId == null).OrderBy(c => c.Name).ToList();
        var byId = cats.ToDictionary(c => c.Id);
        
        var result = new List<SelectListItem>();
        
        foreach (var parent in parents)
        {
            // Add parent category
            result.Add(new SelectListItem
            {
                Value = parent.Id.ToString(),
                Text = parent.Name,
                Selected = selected == parent.Id
            });
            
            // Add children categories with indentation
            var children = cats.Where(c => c.ParentId == parent.Id).OrderBy(c => c.Name).ToList();
            foreach (var child in children)
            {
                result.Add(new SelectListItem
                {
                    Value = child.Id.ToString(),
                    Text = $"  {child.Name}",
                    Selected = selected == child.Id
                });
            }
        }
        
        return result;
    }

    private async Task<List<SelectListItem>> BuildLocationSelectAsync(int? selected = null)
    {
        var locs = await _db.Location.AsNoTracking().OrderBy(l => l.Building).ThenBy(l => l.Name).ToListAsync();
        return locs
            .Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = string.IsNullOrEmpty(l.Building) ? l.Name : $"{l.Building} - {l.Name}",
                Selected = selected == l.Id
            })
            .ToList();
    }
}
