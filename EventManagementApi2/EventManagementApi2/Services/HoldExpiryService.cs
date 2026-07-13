namespace EventManagementApi2.Services;

/// <summary>
/// Background worker that periodically returns inventory from holds that were never
/// confirmed (e.g. the buyer abandoned checkout). This keeps available counts accurate.
/// </summary>
public sealed class HoldExpiryService : BackgroundService
{
    private readonly IInventoryService _inventory;
    private readonly ILogger<HoldExpiryService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public HoldExpiryService(IInventoryService inventory, ILogger<HoldExpiryService> logger)
    {
        _inventory = inventory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var released = await _inventory.ReleaseExpiredHoldsAsync();
                if (released > 0)
                    _logger.LogInformation("Released {Count} expired ticket hold(s)", released);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while releasing expired holds");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
