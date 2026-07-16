namespace LostAndFound.Models.ViewModels.Admin;

public class AuditLogFilterViewModel
{
    public string? ActorUserId { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsPublic { get; set; }
}