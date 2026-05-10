using TalentPilot.Api.Services;

namespace TalentPilot.Api.Services;

/// <summary>
/// 简历采集定时调度器 - 每6小时自动采集所有活跃渠道的新简历
/// </summary>
public class ResumeCollectionScheduler : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ResumeCollectionScheduler> _logger;
    private Timer? _timer;

    public ResumeCollectionScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<ResumeCollectionScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("简历采集定时调度器启动，间隔: 6小时");

        // 立即执行一次，然后每6小时执行
        _timer = new Timer(Execute, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(6));

        return Task.CompletedTask;
    }

    private async void Execute(object? state)
    {
        _logger.LogInformation("定时采集任务开始执行");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IResumeCollectionService>();
            await service.CollectAllActiveChannelsAsync();
            _logger.LogInformation("定时采集任务完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定时采集任务执行失败");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("简历采集定时调度器停止");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}