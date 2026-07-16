namespace LostAndFound.Models.ViewModels.Admin;

public class DashboardViewModel
{
    public int TotalFoundItems { get; set; }
    public int ReturnedItems { get; set; }
    public double ReturnRate { get; set; }
    public double AverageReturnDays { get; set; }
    public int LongestUnclaimedDays { get; set; }
    public List<TopFinderViewModel> TopFinders { get; set; } = new();
    public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
}

public class TopFinderViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int ItemsFound { get; set; }
}

public class RecentActivityViewModel
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? ActorName { get; set; }
    public DateTime CreatedAt { get; set; }
}