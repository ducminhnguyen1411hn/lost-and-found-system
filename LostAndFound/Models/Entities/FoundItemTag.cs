using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class FoundItemTag
{
    public int Id { get; set; }

    public int FoundItemId { get; set; }

    public int TagId { get; set; }

    public virtual FoundItem FoundItem { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}
