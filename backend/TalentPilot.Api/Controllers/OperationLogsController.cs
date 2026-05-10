using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// OperationLogs
/// </summary>
[ApiController]
[Route("api/operation-logs")]
[Authorize(Roles = "admin")]
public class OperationLogsController : ControllerBase
{
    private readonly OperationLogService _logService;

    public OperationLogsController(OperationLogService logService)
    {
        _logService = logService;
    }

    [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> GetLogs(
        [FromQuery] long? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new OperationLogQuery(userId, action, entityType, startDate, endDate, page, pageSize);
        var (items, total) = await _logService.QueryLogs(query);

        var result = items.Select(l => new
        {
            l.Id,
            l.UserId,
            UserName = l.User?.FullName ?? l.User?.Username ?? "Unknown",
            l.Action,
            l.EntityType,
            l.EntityId,
            l.Detail,
            l.IpAddress,
            l.UserAgent,
            l.CreatedAt
        });

        var response = new
        {
            total,
            page,
            pageSize,
            items = result
        };

        return Ok(new ApiResponse<object>(true, "获取成功", response));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> GetLog(long id)
    {
        var log = await _logService.GetLogById(id);
        if (log == null)
        {
            return NotFound(new ApiResponse<object>(false, "日志不存在", null));
        }

        var result = new
        {
            log.Id,
            log.UserId,
            UserName = log.User?.FullName ?? log.User?.Username ?? "Unknown",
            log.Action,
            log.EntityType,
            log.EntityId,
            log.Detail,
            log.IpAddress,
            log.UserAgent,
            log.CreatedAt
        };

        return Ok(new ApiResponse<object>(true, "获取成功", result));
    }
}