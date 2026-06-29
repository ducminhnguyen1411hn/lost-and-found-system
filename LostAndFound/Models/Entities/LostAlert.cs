using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class LostAlert
{
    public int Id { get; set; }

    public string OwnerUserId { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? LocationId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? Keyword { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Location? Location { get; set; }

    public virtual ICollection<LostAlertTag> LostAlertTag { get; set; } = new List<LostAlertTag>();
}
