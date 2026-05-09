using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/distribution")]
[Authorize]
public class JobDistributionController : ControllerBase
{
    private readonly JobDistributionService _svc;
    public JobDistributionController(JobDistributionService svc) => _svc = svc;

    [HttpPost("trigger")]
    public async Task<IActionResult> TriggerDistribution([FromBody] TriggerDistributionDto dto)
    {
        if (dto.JobPostId <= 0 || dto.ChannelTypes == null || dto.ChannelTypes.Count == 0)
            return BadRequest(new ApiResponse<object>(false, "参数错误", null));

        var result = await _svc.TriggerDistributionAsync(dto.JobPostId, dto.ChannelTypes);
        return Ok(new ApiResponse<object>(true, result.Message, result.Tasks));
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTasks([FromBody] CreateDistributionTaskDto dto)
    {
        if (dto.JobPostId <= 0 || dto.ChannelTypes == null || dto.ChannelTypes.Count == 0)
            return BadRequest(new ApiResponse<object>(false, "参数错误", null));

        var tasks = await _svc.CreateTasksAsync(dto.JobPostId, dto.ChannelTypes, dto.ScheduledAt);
        return Ok(new ApiResponse<object>(true, $"已创建 {tasks.Count} 个分发任务", tasks));
    }

    [HttpGet("tasks/{id}")]
    public async Task<IActionResult> GetTask(long id)
    {
        var task = await _svc.GetTaskByIdAsync(id);
        if (task == null) return NotFound(new ApiResponse<object>(false, "任务不存在", null));
        return Ok(new ApiResponse<object>(true, "获取成功", task));
    }

    [HttpGet("tasks/job/{jobPostId}")]
    public async Task<IActionResult> GetTasksByJob(int jobPostId)
    {
        var tasks = await _svc.GetTasksByJobAsync(jobPostId);
        return Ok(new ApiResponse<object>(true, "获取成功", tasks));
    }

    [HttpDelete("tasks/{id}")]
    public async Task<IActionResult> CancelTask(long id)
    {
        var ok = await _svc.CancelTaskAsync(id);
        if (!ok) return BadRequest(new ApiResponse<object>(false, "任务无法取消（仅待发布状态可取消）", null));
        return Ok(new ApiResponse<object>(true, "任务已取消", null));
    }

    [HttpGet("tasks/{id}/logs")]
    public async Task<IActionResult> GetTaskLogs(long id)
    {
        var logs = await _svc.GetTaskLogsAsync(id);
        return Ok(new ApiResponse<object>(true, "获取成功", logs));
    }

    [HttpGet("logs/job/{jobPostId}")]
    public async Task<IActionResult> GetJobDistributionLogs(int jobPostId)
    {
        var logs = await _svc.GetJobDistributionLogsAsync(jobPostId);
        return Ok(new ApiResponse<object>(true, "获取成功", logs));
    }

    [HttpPut("tasks/{id}/retry")]
    public async Task<IActionResult> RetryTask(long id)
    {
        var ok = await _svc.RetryTaskAsync(id);
        if (!ok) return BadRequest(new ApiResponse<object>(false, "任务无法重试", null));
        return Ok(new ApiResponse<object>(true, "任务已重试", null));
    }
}
