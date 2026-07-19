using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class ClaimImage
{
    public int Id { get; set; }

    public int ClaimId { get; set; }

    public string Url { get; set; } = null!;

    public int SortOrder { get; set; }

    public virtual Claim Claim { get; set; } = null!;
}
