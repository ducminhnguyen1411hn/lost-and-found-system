using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class FoundItemImage
{
    public int Id { get; set; }

    public int FoundItemId { get; set; }

    public string Url { get; set; } = null!;

    public int SortOrder { get; set; }

    public virtual FoundItem FoundItem { get; set; } = null!;
}
