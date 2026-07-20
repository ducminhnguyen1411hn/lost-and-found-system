using LostAndFound.Services.Interfaces;

namespace LostAndFound.Services;

/// <summary>The "cron" for FR-HOLD/aging: once a day it runs the same <see cref="IUnclaimedSweepService"/>
/// the admin button calls, so overdue items get marked Unclaimed even when no one clicks. Runs while the app
/// is up; the manual button covers on-demand / demo. Resolves the scoped service inside a fresh DI scope.</summary>
public class UnclaimedSweepBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnclaimedSweepBackgroundService> _logger;

    public UnclaimedSweepBackgroundService(IServiceScopeFactory scopeFactory, ILogger<UnclaimedSweepBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sweep = scope.ServiceProvider.GetRequiredService<IUnclaimedSweepService>();
                var marked = await sweep.SweepOverdueAsync(); // null actor = system
                if (marked > 0)
                    _logger.LogInformation("Unclaimed sweep marked {Count} overdue item(s) as unclaimed.", marked);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unclaimed sweep failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { break; } // app shutting down
        }
    }
}
