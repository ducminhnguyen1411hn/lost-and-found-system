using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class ThankYou
{
    public int Id { get; set; }

    public int FoundItemId { get; set; }

    public string FromUserId { get; set; } = null!;

    public string ToUserId { get; set; } = null!;

    public int Rating { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual FoundItem FoundItem { get; set; } = null!;
}
