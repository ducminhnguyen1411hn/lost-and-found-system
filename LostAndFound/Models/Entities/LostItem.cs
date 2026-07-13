using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class LostItem
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int LocationId { get; set; }

    public DateTime LostAt { get; set; }

    public int Status { get; set; }

    public string OwnerUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;

    public virtual ICollection<LostItemImage> LostItemImage { get; set; } = new List<LostItemImage>();

    public virtual ICollection<LostItemTag> LostItemTag { get; set; } = new List<LostItemTag>();
}
