using TrainTicketSystem.Services;

namespace TrainTicketSystem.Jobs;

/// <summary>
/// Runs every 60 seconds — finds seats whose hold has expired and releases them.
/// Uses IServiceScopeFactory because ISeatService is Scoped, not Singleton.
/// </summary>
public class SeatHoldCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeatHoldCleanupJob> _logger;

    public SeatHoldCleanupJob(IServiceScopeFactory scopeFactory, ILogger<SeatHoldCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SeatHoldCleanupJob started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var seatService = scope.ServiceProvider.GetRequiredService<ISeatService>();
                    await seatService.ReleaseExpiredHoldsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error releasing expired seat holds.");
                }

                // Wait 60 seconds before next cleanup
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // BackgroundService is stopping
        }
    }
}
