namespace LostAndFound.Models.ViewModels.Notifications;

public class NotificationItemViewModel
{
    public int Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string? LinkUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}
