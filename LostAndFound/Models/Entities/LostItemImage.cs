using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class LostItemImage
{
    public int Id { get; set; }

    public int LostItemId { get; set; }

    public string Url { get; set; } = null!;

    public int SortOrder { get; set; }

    public virtual LostItem LostItem { get; set; } = null!;
}
