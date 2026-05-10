using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// AIInterviewSessions
/// </summary>
[ApiController]
[Route("api/ai-interview-sessions")]
public class AIInterviewSessionsController : ControllerBase
{
    private readonly AIInterviewSessionService _sessionService;
    private readonly OperationLogService _logService;
    private readonly TalentPilotDbContext _context;

    public AIInterviewSessionsController(
        AIInterviewSessionService sessionService,
        OperationLogService logService,
        TalentPilotDbContext context)
    {
        _sessionService = sessionService;
        _logService = logService;
        _context = context;
    }

    [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult<ApiResponse<object>>> GetSessions(
        [FromQuery] string? status = null,
        [FromQuery] long? candidateId = null,
        [FromQuery] int? jobPostId = null,
        [FromQuery] int? invitationId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _sessionService.GetAllAsync(status, candidateId, jobPostId, invitationId, dateFrom, dateTo, page, pageSize);

        return Ok(new ApiResponse<object>(true, "获取成功", new
        {
            total,
            page,
            pageSize,
            items = items.Select(s => new AIInterviewSessionResponse
            {
                Id = s.Id,
                InterviewInvitationId = s.InterviewInvitationId,
                CandidateId = s.CandidateId,
                CandidateName = s.Candidate?.Name,
                JobPostId = s.JobPostId,
                JobPostTitle = s.JobPost?.Title,
                SessionToken = s.SessionToken,
                Status = s.Status,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                DurationSeconds = s.DurationSeconds,
                RecordingUrl = s.RecordingUrl,
                Transcript = s.Transcript,
                OverallScore = s.OverallScore,
                AiComments = s.AiComments,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
        }));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,hr,hiring_manager")]
    public async Task<ActionResult<ApiResponse<object>>> GetSession(int id)
    {
        var session = await _sessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound(new ApiResponse<object>(false, "会话不存在", null));

        return Ok(new ApiResponse<object>(true, "获取成功", new AIInterviewSessionResponse
        {
            Id = session.Id,
            InterviewInvitationId = session.InterviewInvitationId,
            CandidateId = session.CandidateId,
            CandidateName = session.Candidate?.Name,
            JobPostId = session.JobPostId,
            JobPostTitle = session.JobPost?.Title,
            SessionToken = session.SessionToken,
            Status = session.Status,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            DurationSeconds = session.DurationSeconds,
            RecordingUrl = session.RecordingUrl,
            Transcript = session.Transcript,
            OverallScore = session.OverallScore,
            AiComments = session.AiComments,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        }));
    }

    [HttpGet("by-token/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetSessionByToken(string token)
    {
        var session = await _sessionService.GetByTokenAsync(token);
        if (session == null)
            return NotFound(new ApiResponse<object>(false, "会话不存在", null));

        return Ok(new ApiResponse<object>(true, "获取成功", new AIInterviewSessionResponse
        {
            Id = session.Id,
            InterviewInvitationId = session.InterviewInvitationId,
            CandidateId = session.CandidateId,
            CandidateName = session.Candidate?.Name,
            JobPostId = session.JobPostId,
            JobPostTitle = session.JobPost?.Title,
            SessionToken = session.SessionToken,
            Status = session.Status,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            DurationSeconds = session.DurationSeconds,
            RecordingUrl = session.RecordingUrl,
            Transcript = session.Transcript,
            OverallScore = session.OverallScore,
            AiComments = session.AiComments,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        }));
    }

    [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult<ApiResponse<object>>> CreateSession([FromBody] CreateAIInterviewSessionRequest request)
    {
        if (request.InterviewInvitationId <= 0)
            return BadRequest(new ApiResponse<object>(false, "面试邀请ID不能为空", null));

        var result = await _sessionService.CreateAsync(request.InterviewInvitationId);
        if (result == null)
            return BadRequest(new ApiResponse<object>(false, "创建失败，面试邀请不存在", null));

        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            await _logService.RecordLog(userId, "CREATE", "AIInterviewSession", result.Id, $"创建AI面试会话 {result.Id}", HttpContext);
        }

        return Created($"/api/ai-interview-sessions/{result.Id}", new ApiResponse<object>(true, "创建成功", result));
    }

    [HttpPut("{id}/start")]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult<ApiResponse<object>>> StartSession(int id)
    {
        var session = await _sessionService.StartAsync(id);
        if (session == null)
            return BadRequest(new ApiResponse<object>(false, "开始失败，会话不存在或状态不允许", null));

        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            await _logService.RecordLog(userId, "START", "AIInterviewSession", id, $"开始AI面试 {id}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "面试已开始", new { session.Id, session.Status, session.StartTime }));
    }

    [HttpPut("{id}/complete")]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult<ApiResponse<object>>> CompleteSession(int id)
    {
        var session = await _sessionService.CompleteAsync(id);
        if (session == null)
            return BadRequest(new ApiResponse<object>(false, "完成失败，会话不存在或状态不允许", null));

        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            await _logService.RecordLog(userId, "COMPLETE", "AIInterviewSession", id, $"完成AI面试 {id}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "面试已完成", new
        {
            session.Id,
            session.Status,
            session.EndTime,
            session.DurationSeconds,
            session.OverallScore,
            session.AiComments
        }));
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult<ApiResponse<object>>> CancelSession(int id)
    {
        var session = await _sessionService.CancelAsync(id);
        if (session == null)
            return BadRequest(new ApiResponse<object>(false, "取消失败，会话不存在或已完成", null));

        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            await _logService.RecordLog(userId, "CANCEL", "AIInterviewSession", id, $"取消AI面试 {id}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "已取消", new { session.Id, session.Status }));
    }

    [HttpPost("{id}/submit-answer")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> SubmitAnswer(int id, [FromBody] SubmitAnswerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Answer))
            return BadRequest(new ApiResponse<object>(false, "回答内容不能为空", null));

        var result = await _sessionService.SubmitAnswerAsync(id, request);

        return Ok(new ApiResponse<object>(true, "提交成功", result));
    }

    [HttpGet("{id}/next-question")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetNextQuestion(int id)
    {
        var question = await _sessionService.GetNextQuestionAsync(id);
        if (question == null)
            return Ok(new ApiResponse<object>(true, "没有更多问题", new { questionId = (string?)null }));

        return Ok(new ApiResponse<object>(true, "获取成功", question));
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet("{id}/slots")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetAvailableSlots(int id)
    {
        var session = await _sessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound(new ApiResponse<object>(false, "Session not found", null));

        var slots = GenerateAvailableSlots();
        return Ok(new ApiResponse<object>(true, "获取成功", new AvailableSlotsResponse
        {
            Slots = slots,
            BookingDeadline = session.BookingDeadline,
            InterviewDuration = session.InterviewDuration
        }));
    }

    [HttpPost("{id}/book")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> BookSlot(int id, [FromBody] BookSlotRequest request)
    {
        var session = await _sessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound(new ApiResponse<object>(false, "Session not found", null));

        if (session.BookingDeadline.HasValue && DateTime.UtcNow > session.BookingDeadline.Value)
            return BadRequest(new ApiResponse<object>(false, "预约已截止", null));

        session.ScheduledAt = request.SlotTime;
        session.Status = AIInterviewSessionStatus.Booked;
        session.StartTime = request.SlotTime;
        session.EndTime = request.SlotTime.AddMinutes(session.InterviewDuration);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object>(true, "预约成功", new { scheduledAt = session.ScheduledAt }));
    }

    [HttpGet("{id}/booking-status")]
    public async Task<ActionResult<ApiResponse<object>>> GetBookingStatus(int id)
    {
        var session = await _sessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound(new ApiResponse<object>(false, "Session not found", null));

        return Ok(new ApiResponse<object>(true, "获取成功", new BookingStatusResponse
        {
            Status = session.Status,
            ScheduledAt = session.ScheduledAt,
            InterviewDuration = session.InterviewDuration,
            JobPostTitle = session.JobPost?.Title
        }));
    }

    private List<DateTime> GenerateAvailableSlots()
    {
        var slots = new List<DateTime>();
        var start = DateTime.UtcNow.Date.AddHours(9);
        var end = DateTime.UtcNow.Date.AddDays(2).AddHours(18);

        for (var dt = start; dt <= end; dt = dt.AddHours(1))
        {
            if (dt.Hour >= 9 && dt.Hour < 12 || dt.Hour >= 14 && dt.Hour < 18)
                slots.Add(dt);
        }
        return slots;
    }
}
