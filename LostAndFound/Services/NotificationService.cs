using LostAndFound.Data;
using LostAndFound.Models;
using LostAndFound.Models.Entities;
using LostAndFound.Models.ViewModels.Notifications;
using LostAndFound.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Services;

public class NotificationService : INotificationService, INotificationQueries
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationService(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task PushAsync(string recipientUserId, string type, string title, string message, string linkUrl)
    {
        _db.Notification.Add(new Notification
        {
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Message = string.IsNullOrWhiteSpace(message) ? null : message,
            LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl,
            IsRead = false

        });
        await _db.SaveChangesAsync();

    }

    public async Task PushToStaffAsync(string type, string title, string message, string linkUrl)
    {
        var staff = await _users.GetUsersInRoleAsync("Staff");
        foreach (var u in staff)
            _db.Notification.Add(new Notification
            {
                RecipientUserId = u.Id,
                Type = type,
                Title = title,
                Message = string.IsNullOrWhiteSpace(message) ? null : message,
                LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl,
                IsRead = false
            });
        await _db.SaveChangesAsync();

    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _db.Notification.AsNoTracking().CountAsync(n => n.RecipientUserId == userId && !n.IsRead);

    public async Task<IReadOnlyList<NotificationItemViewModel>> GetRecentAsync(string userId, int take)
    {
        return await _db.Notification.AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationItemViewModel
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> MarkReadAsync(int id, string userId)
    {
        var n = await _db.Notification.FirstOrDefaultAsync(x => x.Id == id && x.RecipientUserId == userId);
        if (n is null) return false;
        if (!n.IsRead) { n.IsRead = true; await _db.SaveChangesAsync(); }
        return true;
    }

    public async Task<int> MarkAllReadAsync(string userId)
    {
        var unread = await _db.Notification.Where(n => n.RecipientUserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        if (unread.Count > 0) await _db.SaveChangesAsync();
        return unread.Count;
    }
}
