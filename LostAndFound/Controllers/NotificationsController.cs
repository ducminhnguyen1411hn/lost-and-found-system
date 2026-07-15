using System.Security.Claims;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationQueries _notifs;
    public NotificationsController(INotificationQueries notifs) => _notifs = notifs;

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var items = await _notifs.GetRecentAsync(Uid, 100);
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _notifs.MarkReadAsync(id, Uid);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifs.MarkAllReadAsync(Uid);
        return RedirectToAction(nameof(Index));
    }
}
