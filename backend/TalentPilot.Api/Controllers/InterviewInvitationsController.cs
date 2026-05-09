using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/interview-invitations")]
[Authorize(Roles = "admin,hr")]
public class InterviewInvitationsController : ControllerBase
{
    private readonly InterviewInvitationService _invitationService;

    public InterviewInvitationsController(InterviewInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,hr,hiring_manager")]
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

    [HttpGet("{id}")]
    [Authorize(Roles = "admin,hr,hiring_manager")]
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

    [HttpGet("by-token/{token}")]
    [AllowAnonymous]
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

    [HttpPost]
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

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateInvitation(int id, [FromBody] UpdateInterviewInvitationRequest request)
    {
        var invitation = await _invitationService.UpdateAsync(id, request);
        if (invitation == null)
            return NotFound(new ApiResponse<object>(false, "邀请不存在或无法更新", null));

        return Ok(new ApiResponse<object>(true, "更新成功", new { invitation.Id }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteInvitation(int id)
    {
        var success = await _invitationService.DeleteAsync(id);
        if (!success)
            return BadRequest(new ApiResponse<object>(false, "邀请不存在或状态不允许删除", null));

        return Ok(new ApiResponse<object>(true, "删除成功", null));
    }

    [HttpPost("{id}/send")]
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

    [HttpPost("{id}/confirm")]
    [AllowAnonymous]
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

    [HttpPost("{id}/refuse")]
    [AllowAnonymous]
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

    [HttpPost("{id}/cancel")]
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
