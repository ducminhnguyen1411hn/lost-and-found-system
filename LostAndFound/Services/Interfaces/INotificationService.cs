namespace LostAndFound.Services.Interfaces;

public interface INotificationService
{
    Task PushAsync(string recipientUserId, string type, string title, string message, string linkUrl);

    Task PushToStaffAsync(string type, string title, string message, string linkUrl);
}
