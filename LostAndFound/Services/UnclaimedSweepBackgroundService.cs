using LostAndFound.Services.Interfaces;

namespace LostAndFound.Services;

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
                var marked = await sweep.SweepOverdueAsync();
                if (marked > 0)
                    _logger.LogInformation("Unclaimed sweep marked {Count} overdue item(s) as unclaimed.", marked);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unclaimed sweep failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}
