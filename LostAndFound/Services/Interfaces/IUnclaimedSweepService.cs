namespace LostAndFound.Services.Interfaces;

/// <summary>Ages out found items nobody has claimed. An item that stays Open past the overdue threshold
/// with no pending claim is flagged Unclaimed, which feeds the admin dispose queue (the Open → Unclaimed
/// transition that was otherwise missing). Triggered both by a Staff/Admin button and a daily background job.</summary>
public interface IUnclaimedSweepService
{
    /// <summary>How many days an item may sit Open before it counts as overdue.</summary>
    int OverdueDays { get; }

    /// <summary>Marks every overdue Open item (no pending claim) as Unclaimed, auditing + notifying the
    /// reporter for each, in one transaction. <paramref name="actorUserId"/> null/empty = system/background.
    /// Returns the number of items marked.</summary>
    Task<int> SweepOverdueAsync(string? actorUserId = null);
}
