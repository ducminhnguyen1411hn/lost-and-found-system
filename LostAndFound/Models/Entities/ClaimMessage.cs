using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class ClaimMessage
{
    public int Id { get; set; }

    public int ClaimId { get; set; }

    public string SenderUserId { get; set; } = null!;

    public string Body { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Claim Claim { get; set; } = null!;
}
