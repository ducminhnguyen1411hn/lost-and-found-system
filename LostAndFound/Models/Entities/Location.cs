using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class Location
{
    public int Id { get; set; }

    public string? Building { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<CameraCheckRequest> CameraCheckRequest { get; set; } = new List<CameraCheckRequest>();

    public virtual ICollection<FoundItem> FoundItem { get; set; } = new List<FoundItem>();

    public virtual ICollection<LostAlert> LostAlert { get; set; } = new List<LostAlert>();
}
