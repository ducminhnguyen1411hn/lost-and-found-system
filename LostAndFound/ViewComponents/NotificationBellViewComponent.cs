using System.Security.Claims;
using LostAndFound.Models.ViewModels.Notifications;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationQueries _notifs;
    public NotificationBellViewComponent(INotificationQueries notifs) => _notifs = notifs;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var uid = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid is null) return Content(string.Empty);

        var unread = await _notifs.GetUnreadCountAsync(uid);
        var recent = await _notifs.GetRecentAsync(uid, 8);
        return View(new BellModel { Unread = unread, Recent = recent });
    }

    public class BellModel
    {
        public int Unread { get; init; }
        public IReadOnlyList<NotificationItemViewModel> Recent { get; init; } = Array.Empty<NotificationItemViewModel>();
    }
}
