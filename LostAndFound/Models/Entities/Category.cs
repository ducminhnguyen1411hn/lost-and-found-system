using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class Category
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<FoundItem> FoundItem { get; set; } = new List<FoundItem>();

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();

    public virtual ICollection<LostAlert> LostAlert { get; set; } = new List<LostAlert>();

    public virtual ICollection<LostItem> LostItem { get; set; } = new List<LostItem>();

    public virtual Category? Parent { get; set; }
}
