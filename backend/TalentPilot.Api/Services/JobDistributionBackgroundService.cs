namespace TalentPilot.Api.Services;

public class JobDistributionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<JobDistributionBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public JobDistributionBackgroundService(IServiceProvider sp, ILogger<JobDistributionBackgroundService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobDistributionBackgroundService started");

        // Run once on startup after a short delay
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        await RunOnceAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in distribution background loop");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunOnceAsync()
    {
        using var scope = _sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<JobDistributionService>();
        await service.ExecuteAllPendingAsync();
    }
}
