using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class LostAlertTag
{
    public int Id { get; set; }

    public int LostAlertId { get; set; }

    public int TagId { get; set; }

    public virtual LostAlert LostAlert { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}
