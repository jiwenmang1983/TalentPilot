using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public class JobDistributionService
{
    private readonly TalentPilotDbContext _db;
    private readonly ILogger<JobDistributionService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private static readonly Dictionary<string, string> ChannelNames = new()
    {
        ["liepin"] = "猎聘",
        ["lagou"] = "拉勾",
        ["boss"] = "Boss直聘",
        ["linkedin"] = "领英",
        ["xiaohongshu"] = "小红书",
        ["custom"] = "自定义"
    };

    private static readonly Dictionary<string, string> StatusTexts = new()
    {
        ["pending"] = "待发布",
        ["running"] = "发布中",
        ["success"] = "已发布",
        ["failed"] = "发布失败"
    };

    public JobDistributionService(TalentPilotDbContext db, ILogger<JobDistributionService> logger, IServiceProvider serviceProvider)
    {
        _db = db;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public static string GetChannelName(string channelType) =>
        ChannelNames.GetValueOrDefault(channelType.ToLower(), channelType);

    public static string GetStatusText(string status) =>
        StatusTexts.GetValueOrDefault(status, status);

    public static string GetStatusColor(string status) => status switch
    {
        "pending" => "blue",
        "running" => "orange",
        "success" => "green",
        "failed" => "red",
        _ => "default"
    };

    public async Task<List<DistributionTaskDto>> CreateTasksAsync(int jobPostId, List<string> channelTypes, DateTime? scheduledAt)
    {
        var tasks = new List<JobDistributionTask>();
        foreach (var ct in channelTypes)
        {
            var existing = await _db.JobDistributionTasks
                .FirstOrDefaultAsync(t => t.JobPostId == jobPostId && t.ChannelType == ct.ToLower() && t.TaskStatus == "pending");

            if (existing != null) continue;

            var task = new JobDistributionTask
            {
                JobPostId = jobPostId,
                ChannelType = ct.ToLower(),
                TaskStatus = "pending",
                ScheduledAt = scheduledAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.JobDistributionTasks.Add(task);
            tasks.Add(task);
        }
        await _db.SaveChangesAsync();

        var job = await _db.JobPosts.FindAsync(jobPostId);
        return tasks.Select(t => ToDto(t, job?.Title ?? "")).ToList();
    }

    public async Task<TriggerResultDto> TriggerDistributionAsync(int jobPostId, List<string> channelTypes)
    {
        var tasks = await CreateTasksAsync(jobPostId, channelTypes, null);

        foreach (var taskDto in tasks)
        {
            // Each task runs in its own scope to have a fresh DbContext
            _ = Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<JobDistributionService>();
                await svc.ExecuteTaskAsync(taskDto.Id);
            });
        }

        return new TriggerResultDto(true, $"已触发 {tasks.Count} 个分发任务", tasks);
    }

    public async Task ExecuteTaskAsync(long taskId)
    {
        var task = await _db.JobDistributionTasks.FindAsync(taskId);
        if (task == null || task.TaskStatus != "pending") return;

        task.TaskStatus = "running";
        task.StartedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await AddLogAsync(taskId, "info", $"开始向 {GetChannelName(task.ChannelType)} 发布职位");

        try
        {
            var content = await _db.JobChannelContents
                .FirstOrDefaultAsync(c => c.JobPostId == task.JobPostId && c.ChannelType == task.ChannelType);

            if (content != null)
            {
                await AddLogAsync(taskId, "info", $"已获取适配内容，字数：{content.AdaptedContent?.Length ?? 0}");
            }

            // Simulate API call delay (2-5 seconds)
            await Task.Delay(Random.Shared.Next(2000, 5001));

            task.TaskStatus = "success";
            task.CompletedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            await AddLogAsync(taskId, "info", $"职位内容已推送至 {GetChannelName(task.ChannelType)}", "模拟模式：实际分发需接入各平台API");
        }
        catch (Exception ex)
        {
            task.TaskStatus = "failed";
            task.FailureReason = ex.Message;
            task.CompletedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            await AddLogAsync(taskId, "error", $"发布失败：{ex.Message}", ex.StackTrace);
            _logger.LogError(ex, "Distribution task {TaskId} failed", taskId);
        }

        await _db.SaveChangesAsync();
    }

    public async Task ExecuteAllPendingAsync()
    {
        var now = DateTime.UtcNow;
        var pendingTasks = await _db.JobDistributionTasks
            .Where(t => t.TaskStatus == "pending" && (t.ScheduledAt == null || t.ScheduledAt <= now))
            .ToListAsync();

        foreach (var task in pendingTasks)
        {
            _ = Task.Run(() => ExecuteTaskAsync(task.Id));
        }
    }

    public async Task<DistributionTaskDto?> GetTaskByIdAsync(long id)
    {
        var task = await _db.JobDistributionTasks.Include(t => t.JobPost).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return null;
        return ToDto(task, task.JobPost?.Title ?? "");
    }

    public async Task<List<DistributionTaskDto>> GetTasksByJobAsync(int jobPostId)
    {
        var tasks = await _db.JobDistributionTasks.Include(t => t.JobPost)
            .Where(t => t.JobPostId == jobPostId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return tasks.Select(t => ToDto(t, t.JobPost?.Title ?? "")).ToList();
    }

    public async Task<bool> CancelTaskAsync(long id)
    {
        var task = await _db.JobDistributionTasks.FindAsync(id);
        if (task == null || task.TaskStatus != "pending") return false;
        task.TaskStatus = "failed";
        task.FailureReason = "用户取消";
        task.CompletedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await AddLogAsync(id, "info", "任务已被用户取消");
        return true;
    }

    public async Task<bool> RetryTaskAsync(long id)
    {
        var task = await _db.JobDistributionTasks.FindAsync(id);
        if (task == null || (task.TaskStatus != "failed" && task.TaskStatus != "cancelled")) return false;
        task.TaskStatus = "pending";
        task.FailureReason = null;
        task.CompletedAt = null;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await AddLogAsync(id, "info", "任务已重新排队");
        return true;
    }

    public async Task<List<DistributionLogDto>> GetTaskLogsAsync(long taskId)
    {
        return await _db.JobDistributionLogs
            .Where(l => l.TaskId == taskId)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new DistributionLogDto(l.Id, l.TaskId, l.LogLevel, l.Message, l.Details, l.CreatedAt))
            .ToListAsync();
    }

    public async Task<List<DistributionLogDto>> GetJobDistributionLogsAsync(int jobPostId)
    {
        var taskIds = await _db.JobDistributionTasks
            .Where(t => t.JobPostId == jobPostId)
            .Select(t => t.Id)
            .ToListAsync();

        return await _db.JobDistributionLogs
            .Where(l => taskIds.Contains(l.TaskId))
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new DistributionLogDto(l.Id, l.TaskId, l.LogLevel, l.Message, l.Details, l.CreatedAt))
            .ToListAsync();
    }

    private async Task AddLogAsync(long taskId, string level, string message, string? details = null)
    {
        _db.JobDistributionLogs.Add(new JobDistributionLog
        {
            TaskId = taskId,
            LogLevel = level,
            Message = message,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private DistributionTaskDto ToDto(JobDistributionTask t, string jobTitle) =>
        new(t.Id, t.JobPostId, jobTitle, t.ChannelType, GetChannelName(t.ChannelType),
            t.TaskStatus, GetStatusText(t.TaskStatus), t.ScheduledAt, t.StartedAt,
            t.CompletedAt, t.FailureReason, t.CreatedAt);
}
