using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class Tag
{
    public int Id { get; set; }

    public string DisplayTag { get; set; } = null!;

    public string NormalizedTag { get; set; } = null!;

    public virtual ICollection<FoundItemTag> FoundItemTag { get; set; } = new List<FoundItemTag>();

    public virtual ICollection<LostAlertTag> LostAlertTag { get; set; } = new List<LostAlertTag>();
}
