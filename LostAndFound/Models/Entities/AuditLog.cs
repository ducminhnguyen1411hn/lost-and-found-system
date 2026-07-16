using System;
using System.Collections.Generic;
using LostAndFound.Models;

namespace LostAndFound.Models.Entities;

public partial class AuditLog
{
    public int Id { get; set; }

    public string? ActorUserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }

    public string? Detail { get; set; }

    public bool IsPublic { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? IpAddress { get; set; }

    // Navigation property (added manually, not in scaffold)
    public virtual ApplicationUser? ActorUser { get; set; }
}
