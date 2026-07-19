using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class LostItemTag
{
    public int Id { get; set; }

    public int LostItemId { get; set; }

    public int TagId { get; set; }

    public virtual LostItem LostItem { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}
