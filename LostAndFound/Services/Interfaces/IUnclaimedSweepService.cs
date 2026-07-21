namespace LostAndFound.Services.Interfaces;

public interface IUnclaimedSweepService
{
    int OverdueDays { get; }

    Task<int> SweepOverdueAsync(string? actorUserId = null);
}
