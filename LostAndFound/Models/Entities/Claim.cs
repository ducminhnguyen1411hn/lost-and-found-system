using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class Claim
{
    public int Id { get; set; }

    public int FoundItemId { get; set; }

    public string ClaimantUserId { get; set; } = null!;

    public string VerificationDetails { get; set; } = null!;

    public string? EvidenceImagePath { get; set; }

    public int Status { get; set; }

    public string? HandledByUserId { get; set; }

    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? HandledAt { get; set; }

    public virtual FoundItem FoundItem { get; set; } = null!;
}
