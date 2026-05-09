using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// 通知管理接口
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "admin,hr")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// 获取通知日志列表（分页）
    /// </summary>
    /// <param name="candidateName">候选人姓名筛选</param>
    /// <param name="notificationType">通知类型筛选：InterviewInvitation, InterviewReminder, Offer</param>
    /// <param name="status">状态筛选：Pending, Sent, Failed</param>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认20</param>
    /// <returns>通知日志列表</returns>
    [HttpGet]
    [Authorize(Roles = "admin,hr")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> GetNotifications(
        [FromQuery] string? candidateName = null,
        [FromQuery] string? notificationType = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _notificationService.GetAllAsync(candidateName, notificationType, status, page, pageSize);

        return Ok(new ApiResponse<object>(true, "获取成功", new
        {
            total,
            page,
            pageSize,
            items
        }));
    }

    /// <summary>
    /// 发送通知
    /// </summary>
    /// <param name="request">发送通知请求</param>
    /// <returns>发送结果</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> SendNotification([FromBody] SendNotificationRequest request)
    {
        if (request.CandidateId <= 0)
            return BadRequest(new ApiResponse<object>(false, "候选人ID不能为空", null));

        if (string.IsNullOrWhiteSpace(request.NotificationType))
            return BadRequest(new ApiResponse<object>(false, "通知类型不能为空", null));

        var success = await _notificationService.SendNotificationAsync(request);
        if (!success)
            return BadRequest(new ApiResponse<object>(false, "发送失败", null));

        return Ok(new ApiResponse<object>(true, "发送成功", null));
    }

    /// <summary>
    /// 获取通知模板列表
    /// </summary>
    /// <returns>模板列表</returns>
    [HttpGet("templates")]
    [Authorize(Roles = "admin,hr")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> GetTemplates()
    {
        var templates = await _notificationService.GetTemplatesAsync();
        return Ok(new ApiResponse<object>(true, "获取成功", templates));
    }

    /// <summary>
    /// 更新通知模板
    /// </summary>
    /// <param name="id">模板ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("templates/{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateTemplate(int id, [FromBody] UpdateNotificationTemplateRequest request)
    {
        if (id <= 0)
            return BadRequest(new ApiResponse<object>(false, "模板ID无效", null));

        var success = await _notificationService.UpdateTemplateAsync(id, request);
        if (!success)
            return BadRequest(new ApiResponse<object>(false, "更新失败", null));

        return Ok(new ApiResponse<object>(true, "更新成功", new { id }));
    }
}