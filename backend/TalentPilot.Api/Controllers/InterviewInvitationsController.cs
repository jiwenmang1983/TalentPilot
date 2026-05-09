using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// 面试邀请管理接口
/// </summary>
[ApiController]
[Route("api/interview-invitations")]
[Authorize(Roles = "admin,hr")]
[Produces("application/json")]
public class InterviewInvitationsController : ControllerBase
{
    private readonly InterviewInvitationService _invitationService;

    public InterviewInvitationsController(InterviewInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    /// <summary>
    /// 获取面试邀请列表（分页）
    /// </summary>
    /// <param name="status">邀请状态筛选：pending/sent/confirmed/refused/cancelled</param>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认20</param>
    /// <returns>邀请列表</returns>
    [HttpGet]
    [Authorize(Roles = "admin,hr,hiring_manager")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> GetInvitations(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var (items, total) = await _invitationService.GetAllAsync(status, page, pageSize, userId);

        return Ok(new ApiResponse<object>(true, "获取成功", new
        {
            total,
            page,
            pageSize,
            items = items.Select(i => new InterviewInvitationResponse
            {
                Id = i.Id,
                CandidateName = i.Candidate?.Name ?? string.Empty,
                CandidatePhone = i.Candidate?.Phone,
                CandidateEmail = i.Candidate?.Email,
                JobPostTitle = i.JobPost?.Title ?? string.Empty,
                InvitedByUserName = i.InvitedByUser?.Username ?? string.Empty,
                InterviewTime = i.InterviewTime,
                TimeSlotStart = i.TimeSlotStart,
                TimeSlotEnd = i.TimeSlotEnd,
                Status = i.Status,
                CreatedAt = i.CreatedAt,
                ConfirmedAt = i.ConfirmedAt,
                RefusedAt = i.RefusedAt,
                Notes = i.Notes,
                InviteToken = i.InviteToken
            })
        }));
    }

    /// <summary>
    /// 获取单个面试邀请详情
    /// </summary>
    /// <param name="id">邀请ID</param>
    /// <returns>邀请详情</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin,hr,hiring_manager")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> GetInvitation(int id)
    {
        var invitation = await _invitationService.GetByIdAsync(id);
        if (invitation == null)
            return NotFound(new ApiResponse<object>(false, "邀请不存在", null));

        return Ok(new ApiResponse<object>(true, "获取成功", new InterviewInvitationResponse
        {
            Id = invitation.Id,
            CandidateName = invitation.Candidate?.Name ?? string.Empty,
            CandidatePhone = invitation.Candidate?.Phone,
            CandidateEmail = invitation.Candidate?.Email,
            JobPostTitle = invitation.JobPost?.Title ?? string.Empty,
            InvitedByUserName = invitation.InvitedByUser?.Username ?? string.Empty,
            InterviewTime = invitation.InterviewTime,
            TimeSlotStart = invitation.TimeSlotStart,
            TimeSlotEnd = invitation.TimeSlotEnd,
            Status = invitation.Status,
            CreatedAt = invitation.CreatedAt,
            ConfirmedAt = invitation.ConfirmedAt,
            RefusedAt = invitation.RefusedAt,
            Notes = invitation.Notes,
            InviteToken = invitation.InviteToken
        }));
    }

    /// <summary>
    /// 通过Token获取面试邀请（候选人端，无需登录）
    /// </summary>
    /// <param name="token">邀请Token</param>
    /// <returns>邀请详情</returns>
    [HttpGet("by-token/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetInvitationByToken(string token)
    {
        var invitation = await _invitationService.GetByTokenAsync(token);
        if (invitation == null)
            return NotFound(new ApiResponse<object>(false, "邀请不存在或链接已失效", null));

        return Ok(new ApiResponse<object>(true, "获取成功", new InterviewInvitationTokenResponse
        {
            Id = invitation.Id,
            CandidateName = invitation.Candidate?.Name ?? string.Empty,
            CandidatePhone = invitation.Candidate?.Phone,
            CandidateEmail = invitation.Candidate?.Email,
            JobPostTitle = invitation.JobPost?.Title ?? string.Empty,
            CompanyName = "TalentPilot",
            InterviewTime = invitation.InterviewTime,
            TimeSlotStart = invitation.TimeSlotStart,
            TimeSlotEnd = invitation.TimeSlotEnd,
            Status = invitation.Status,
            InviteSentAt = invitation.InviteSentAt
        }));
    }

    /// <summary>
    /// 创建面试邀请
    /// </summary>
    /// <param name="request">创建邀请请求</param>
    /// <returns>新创建的邀请</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> CreateInvitation([FromBody] CreateInterviewInvitationRequest request)
    {
        if (request.CandidateId <= 0)
            return BadRequest(new ApiResponse<object>(false, "候选人ID不能为空", null));

        if (request.JobPostId <= 0)
            return BadRequest(new ApiResponse<object>(false, "职位ID不能为空", null));

        if (request.TimeSlotEnd <= request.TimeSlotStart)
            return BadRequest(new ApiResponse<object>(false, "时间窗口结束时间必须晚于开始时间", null));

        var userId = GetCurrentUserId();
        var result = await _invitationService.CreateAsync(request, userId);

        if (result == null)
            return BadRequest(new ApiResponse<object>(false, "创建失败", null));

        return Created($"/api/interview-invitations/{result.Id}", new ApiResponse<object>(true, "创建成功", result));
    }

    /// <summary>
    /// 更新面试邀请
    /// </summary>
    /// <param name="id">邀请ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateInvitation(int id, [FromBody] UpdateInterviewInvitationRequest request)
    {
        var invitation = await _invitationService.UpdateAsync(id, request);
        if (invitation == null)
            return NotFound(new ApiResponse<object>(false, "邀请不存在或无法更新", null));

        return Ok(new ApiResponse<object>(true, "更新成功", new { invitation.Id }));
    }

    /// <summary>
    /// 删除面试邀请
    /// </summary>
    /// <param name="id">邀请ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteInvitation(int id)
    {
        var success = await _invitationService.DeleteAsync(id);
        if (!success)
            return BadRequest(new ApiResponse<object>(false, "邀请不存在或状态不允许删除", null));

        return Ok(new ApiResponse<object>(true, "删除成功", null));
    }

    /// <summary>
    /// 发送面试邀请（生成链接并发送）
    /// </summary>
    /// <param name="id">邀请ID</param>
    /// <returns>发送结果</returns>
    [HttpPost("{id}/send")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> SendInvitation(int id)
    {
        var invitation = await _invitationService.SendInviteAsync(id);
        if (invitation == null)
            return BadRequest(new ApiResponse<object>(false, "邀请不存在或状态不允许发送", null));

        return Ok(new ApiResponse<object>(true, "发送成功", new
        {
            invitation.Id,
            invitation.InviteToken,
            invitation.InviteSentAt
        }));
    }

    /// <summary>
    /// 候选人确认参加面试
    /// </summary>
    /// <param name="id">邀请ID</param>
    /// <param name="request">确认请求（可选：调整面试时间）</param>
    /// <returns>确认结果</returns>
    [HttpPost("{id}/confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmInvitation(int id, [FromBody] InvitationConfirmRequest? request)
    {
        var invitation = await _invitationService.ConfirmAsync(id, request?.InterviewTime);
        if (invitation == null)
            return BadRequest(new ApiResponse<object>(false, "邀请不存在或状态不允许确认", null));

        return Ok(new ApiResponse<object>(true, "确认成功", new
        {
            invitation.Id,
            invitation.Status,
            invitation.ConfirmedAt
        }));
    }

    /// <summary>
    /// 候选人拒绝面试邀请
    /// </summary>
    /// <param name="id">邀请ID</param>
    /// <returns>拒绝结果</returns>
    [HttpPost("{id}/refuse")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> RefuseInvitation(int id)
    {
        var invitation = await _invitationService.RefuseAsync(id);
        if (invitation == null)
            return BadRequest(new ApiResponse<object>(false, "邀请不存在或状态不允许拒绝", null));

        return Ok(new ApiResponse<object>(true, "已拒绝", new
        {
            invitation.Id,
            invitation.Status,
            invitation.RefusedAt
        }));
    }

    /// <summary>
    /// HR/管理员取消面试邀请
    /// </summary>
    /// <param name="id">邀请ID</param>
    /// <returns>取消结果</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> CancelInvitation(int id)
    {
        var invitation = await _invitationService.CancelAsync(id);
        if (invitation == null)
            return BadRequest(new ApiResponse<object>(false, "邀请不存在或状态不允许取消（已确认的面试需在24小时前取消）", null));

        return Ok(new ApiResponse<object>(true, "取消成功", new
        {
            invitation.Id,
            invitation.Status
        }));
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}

public class InvitationConfirmRequest
{
    public DateTime? InterviewTime { get; set; }
}
