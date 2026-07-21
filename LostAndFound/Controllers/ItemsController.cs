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

    private async Task<bool> IsPostingBlockedAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return false;
        var user = await _db.Users.FindAsync(Uid);
        return user is not null && user.IsPostingBlocked;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(BoardSearchViewModel q)
    {
        q.Results = await _board.SearchAsync(q);
        await FillLookupsAsync(l => q.Categories = l, l => q.Locations = l);
        ViewData["IsPostingBlocked"] = await IsPostingBlockedAsync();
        return View(q);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mine(BoardSearchViewModel q)
    {
        q.Results = await _board.SearchAsync(q, Uid);
        await FillLookupsAsync(l => q.Categories = l, l => q.Locations = l);
        ViewData["IsPostingBlocked"] = await IsPostingBlockedAsync();
        return View(q);
    }

    [Authorize(Roles = "Member,Staff,Admin")]
    [HttpGet]
    public async Task<IActionResult> Create(ItemKind kind = ItemKind.Found)
    {
        if (await IsPostingBlockedAsync())
        {
            TempData["ErrorMessage"] = "Bạn đã bị chặn đăng bài. Vui lòng liên hệ quản trị viên.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new ItemCreateViewModel { Kind = kind };
        await FillLookupsAsync(l => vm.Categories = l, l => vm.Locations = l);
        return View(vm);
    }

    [Authorize(Roles = "Member,Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemCreateViewModel vm)
    {
        if (await IsPostingBlockedAsync())
        {
            TempData["ErrorMessage"] = "Bạn đã bị chặn đăng bài. Vui lòng liên hệ quản trị viên.";
            return RedirectToAction(nameof(Index));
        }

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
