using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class Notification
{
    public int Id { get; set; }

    public string RecipientUserId { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
