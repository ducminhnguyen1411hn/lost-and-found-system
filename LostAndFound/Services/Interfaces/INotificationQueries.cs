using LostAndFound.Models.ViewModels.Notifications;

namespace LostAndFound.Services.Interfaces;

public interface INotificationQueries
{
    Task<int> GetUnreadCountAsync(string userId);
    Task<IReadOnlyList<NotificationItemViewModel>> GetRecentAsync(string userId, int take);
    Task<bool> MarkReadAsync(int id, string userId);
    Task<int> MarkAllReadAsync(string userId);
}
