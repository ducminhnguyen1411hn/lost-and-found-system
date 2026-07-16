using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models.Enums;
using LostAndFound.Models.ViewModels.FoundItems;
using LostAndFound.Models.ViewModels.Items;
using LostAndFound.Models.ViewModels.LostItems;
using LostAndFound.Services.Images;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Controllers;

/// <summary>The unified board + the merged create form. Detail/Edit/Delete stay on
/// FoundItemsController / LostItemsController — those flows are genuinely different.</summary>
public class ItemsController : Controller
{
    private readonly IItemBoardService _board;
    private readonly IFoundItemService _found;
    private readonly ILostItemService _lost;
    private readonly ApplicationDbContext _db;

    public ItemsController(IItemBoardService board, IFoundItemService found, ILostItemService lost, ApplicationDbContext db)
    {
        _board = board;
        _found = found;
        _lost = lost;
        _db = db;
    }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(BoardSearchViewModel q)
    {
        q.Results = await _board.SearchAsync(q);
        await FillLookupsAsync(l => q.Categories = l, l => q.Locations = l);
        return View(q);
    }

    /// <summary>The signed-in user's own posts, in every status. The owner id comes from the SIGNED-IN
    /// user — never from the query string, or anyone could read another user's hidden (non-Open) posts.</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mine(BoardSearchViewModel q)
    {
        q.Results = await _board.SearchAsync(q, Uid);
        await FillLookupsAsync(l => q.Categories = l, l => q.Locations = l);
        return View(q);
    }

    [Authorize(Roles = "Member,Staff,Admin")]
    [HttpGet]
    public async Task<IActionResult> Create(ItemKind kind = ItemKind.Found)
    {
        var vm = new ItemCreateViewModel { Kind = kind };
        await FillLookupsAsync(l => vm.Categories = l, l => vm.Locations = l);
        return View(vm);
    }

    [Authorize(Roles = "Member,Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(l => vm.Categories = l, l => vm.Locations = l);
            return View(vm);
        }

        try
        {
            if (vm.Kind == ItemKind.Found)
            {
                var f = new FoundItemCreateViewModel
                {
                    Title = vm.Title,
                    Description = vm.Description,
                    CategoryId = vm.CategoryId,
                    LocationId = vm.LocationId,
                    FoundAt = vm.OccurredAt,
                    HoldingType = vm.HoldingType,
                    PrivateMarks = vm.PrivateMarks,
                    TagsRaw = vm.TagsRaw,
                    CoverImage = vm.CoverImage,
                    OtherImages = vm.OtherImages
                };
                var id = await _found.CreateAsync(f, Uid);
                TempData["SuccessMessage"] = "Đã đăng đồ nhặt được.";
                return RedirectToAction("Details", "FoundItems", new { id });
            }
            else
            {
                var l = new LostItemCreateViewModel
                {
                    Title = vm.Title,
                    Description = vm.Description,
                    CategoryId = vm.CategoryId,
                    LocationId = vm.LocationId,
                    LostAt = vm.OccurredAt,
                    TagsRaw = vm.TagsRaw,
                    CoverImage = vm.CoverImage,
                    OtherImages = vm.OtherImages
                };
                var id = await _lost.CreateAsync(l, Uid);
                TempData["SuccessMessage"] = "Đã đăng đồ bị mất.";
                return RedirectToAction("Details", "LostItems", new { id });
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ImageUploadException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await FillLookupsAsync(l => vm.Categories = l, l => vm.Locations = l);
            return View(vm);
        }
    }

    /// <summary>Category (parent-grouped, "Parent › Child") + Location ("Building - Name") dropdowns —
    /// mirrors FoundItemsController/LostItemsController's BuildCategorySelectAsync/BuildLocationSelectAsync
    /// exactly, so the merged create page's dropdowns look identical to the pages it replaces.</summary>
    private async Task FillLookupsAsync(Action<IEnumerable<SelectListItem>> setCats, Action<IEnumerable<SelectListItem>> setLocs)
    {
        var cats = await _db.Category.AsNoTracking().ToListAsync();
        var byId = cats.ToDictionary(c => c.Id);
        setCats(cats
            .OrderBy(c => c.ParentId == null ? c.Name : byId[c.ParentId.Value].Name)
            .ThenBy(c => c.ParentId == null ? string.Empty : c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.ParentId != null && byId.TryGetValue(c.ParentId.Value, out var p) ? $"{p.Name} › {c.Name}" : c.Name
            })
            .ToList());

        var locs = await _db.Location.AsNoTracking().OrderBy(l => l.Building).ThenBy(l => l.Name).ToListAsync();
        setLocs(locs
            .Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = string.IsNullOrEmpty(l.Building) ? l.Name : $"{l.Building} - {l.Name}"
            })
            .ToList());
    }
}
