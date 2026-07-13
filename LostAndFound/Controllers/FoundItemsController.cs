using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models.ViewModels.FoundItems;
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
    [Authorize(Roles = "Member,Staff,Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new FoundItemCreateViewModel
        {
            FoundAt = DateTime.Now,
            Categories = await BuildCategorySelectAsync(),
            Locations = await BuildLocationSelectAsync()
        };
        return View(vm);
    }

    // POST /FoundItems/Create
    [Authorize(Roles = "Member,Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FoundItemCreateViewModel vm)
    {
        if (!ModelState.IsValid)
            return await RedisplayCreate(vm);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var id = await _service.CreateAsync(vm, userId);
            TempData["SuccessMessage"] = "Đã đăng đồ nhặt được thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ImageUploadException ex)
        {
            ModelState.AddModelError(nameof(FoundItemCreateViewModel.ImageFile), ex.Message);
            return await RedisplayCreate(vm);
        }
    }

    private async Task<IActionResult> RedisplayCreate(FoundItemCreateViewModel vm)
    {
        vm.Categories = await BuildCategorySelectAsync(vm.CategoryId);
        vm.Locations = await BuildLocationSelectAsync(vm.LocationId);
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
