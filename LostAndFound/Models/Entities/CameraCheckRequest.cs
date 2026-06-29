using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class CameraCheckRequest
{
    public int Id { get; set; }

    public string RequesterUserId { get; set; } = null!;

    public int LocationId { get; set; }

    public DateTime FromTime { get; set; }

    public DateTime ToTime { get; set; }

    public string ItemDescription { get; set; } = null!;

    public int Status { get; set; }

    public string? HandledByStaffId { get; set; }

    public string? ResponseNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? HandledAt { get; set; }

    public virtual Location Location { get; set; } = null!;
}
