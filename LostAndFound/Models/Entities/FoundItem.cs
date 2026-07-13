using System;
using System.Collections.Generic;

namespace LostAndFound.Models.Entities;

public partial class FoundItem
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int LocationId { get; set; }

    public DateTime FoundAt { get; set; }

    public int Status { get; set; }

    public int HoldingType { get; set; }

    public string? StorageLocation { get; set; }

    public string? PrivateMarks { get; set; }

    public string ReporterUserId { get; set; } = null!;

    public string? CustodianStaffId { get; set; }

    public bool HolderConfirmedHandover { get; set; }

    public bool ClaimantConfirmedHandover { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Claim> Claim { get; set; } = new List<Claim>();

    public virtual ICollection<FoundItemImage> FoundItemImage { get; set; } = new List<FoundItemImage>();

    public virtual ICollection<FoundItemTag> FoundItemTag { get; set; } = new List<FoundItemTag>();

    public virtual Location Location { get; set; } = null!;

    public virtual ThankYou? ThankYou { get; set; }
}
