using TalentPilot.Api.Data;

namespace TalentPilot.Api.Services;

public class DistributionTaskScheduler : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DistributionTaskScheduler> _logger;
    private Timer? _timer;
    private bool _disposed;

    public DistributionTaskScheduler(IServiceScopeFactory scopeFactory, ILogger<DistributionTaskScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DistributionTaskScheduler starting, interval: 30 seconds");
        _timer = new Timer(Execute, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
    }

    private async void Execute(object? state)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TalentPilotDbContext>();
            var service = new JobDistributionService(db,
                scope.ServiceProvider.GetRequiredService<ILogger<JobDistributionService>>(),
                scope.ServiceProvider);
            await service.ExecuteAllPendingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing pending distribution tasks");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DistributionTaskScheduler stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose();
    }
}