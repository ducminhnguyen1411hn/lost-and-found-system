using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.LostItems;
using LostAndFound.Services;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Controllers;

/// <summary>Thin controller for the public lost-item board (mirrors FoundItemsController).</summary>
public class LostItemsController : Controller
{
    private readonly ILostItemService _service;
    private readonly ApplicationDbContext _db;

    public LostItemsController(ILostItemService service, ApplicationDbContext db)
    {
        _service = service;
        _db = db;
    }

    /// <summary>True when an admin has flagged the signed-in user IsPostingBlocked. This direct
    /// /LostItems/Create path is legacy (the nav routes through Items/Create) but still reachable.</summary>
    private async Task<bool> IsPostingBlockedAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _db.Users.FindAsync(userId);
        return user is not null && user.IsPostingBlocked;
    }

    // GET /LostItems — replaced by the unified board; keep a permanent redirect for old links/bookmarks.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index() => RedirectToActionPermanent("Index", "Items", new { kind = ItemKind.Lost });

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _service.GetDetailAsync(id, User);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = "Member,Staff,Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (await IsPostingBlockedAsync())
        {
            TempData["ErrorMessage"] = "Bạn đã bị chặn đăng bài. Vui lòng liên hệ quản trị viên.";
            return RedirectToAction("Index", "Items");
        }

        var vm = new LostItemCreateViewModel
        {
            LostAt = AppTime.LocalNow,
            Categories = await BuildCategorySelectAsync(),
            Locations = await BuildLocationSelectAsync()
        };
        return View(vm);
    }

    [Authorize(Roles = "Member,Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LostItemCreateViewModel vm)
    {
        if (await IsPostingBlockedAsync())
        {
            TempData["ErrorMessage"] = "Bạn đã bị chặn đăng bài. Vui lòng liên hệ quản trị viên.";
            return RedirectToAction("Index", "Items");
        }

        if (!ModelState.IsValid) return await RedisplayCreate(vm);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var id = await _service.CreateAsync(vm, userId);
            TempData["SuccessMessage"] = "Đã đăng đồ bị mất.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ImageUploadException ex)
        {
            ModelState.AddModelError(nameof(LostItemCreateViewModel.CoverImage), ex.Message);
            return await RedisplayCreate(vm);
        }
    }

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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LostItemEditViewModel vm)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return await RedisplayEdit(vm);

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
            ModelState.AddModelError(nameof(LostItemEditViewModel.NewImages), ex.Message);
            return await RedisplayEdit(vm);
        }
    }

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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var ok = await _service.MarkResolvedAsync(id, userId);
        if (!ok) return NotFound();
        TempData["SuccessMessage"] = "Đã đánh dấu tìm thấy.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> RedisplayCreate(LostItemCreateViewModel vm)
    {
        vm.Categories = await BuildCategorySelectAsync(vm.CategoryId);
        vm.Locations = await BuildLocationSelectAsync(vm.LocationId);
        return View(vm);
    }

    private async Task<IActionResult> RedisplayEdit(LostItemEditViewModel vm)
    {
        vm.Categories = await BuildCategorySelectAsync(vm.CategoryId);
        vm.Locations = await BuildLocationSelectAsync(vm.LocationId);
        vm.ExistingImages = await _db.LostItemImage.AsNoTracking()
            .Where(im => im.LostItemId == vm.Id)
            .OrderBy(im => im.SortOrder)
            .Select(im => new LostItemEditViewModel.ImageItem { Id = im.Id, Url = im.Url })
            .ToListAsync();
        return View(vm);
    }

    private async Task<List<SelectListItem>> BuildCategorySelectAsync(int? selected = null)
    {
        var cats = await _db.Category.AsNoTracking().ToListAsync();
        var byId = cats.ToDictionary(c => c.Id);
        return cats
            .OrderBy(c => c.ParentId == null ? c.Name : byId[c.ParentId.Value].Name)
            .ThenBy(c => c.ParentId == null ? string.Empty : c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.ParentId != null && byId.TryGetValue(c.ParentId.Value, out var p) ? $"{p.Name} › {c.Name}" : c.Name,
                Selected = selected == c.Id
            })
            .ToList();
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
