using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class CandidatesController : ControllerBase
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly CandidateConsentService _consentService;
    private readonly OperationLogService _logService;

    public CandidatesController(
        TalentPilotDbContext dbContext,
        CandidateConsentService consentService,
        OperationLogService logService)
    {
        _dbContext = dbContext;
        _consentService = consentService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetCandidates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null)
    {
        var query = _dbContext.Candidates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(c => c.Name.Contains(keyword) || c.Email.Contains(keyword));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new {
                c.Id,
                c.Name,
                c.Email,
                c.Phone,
                c.CurrentPosition,
                c.CurrentCompany,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(new ApiResponse<object>(true, "获取成功", new {
            total,
            page,
            pageSize,
            items
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetCandidate(long id)
    {
        var candidate = await _dbContext.Candidates.FindAsync(id);
        if (candidate == null)
            return NotFound(new ApiResponse<object>(false, "候选人不存在", null));

        return Ok(new ApiResponse<object>(true, "获取成功", candidate));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateCandidate([FromBody] CreateCandidateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new ApiResponse<object>(false, "邮箱不能为空", null));

        var exists = await _dbContext.Candidates.AnyAsync(c => c.Email == request.Email);
        if (exists)
            return BadRequest(new ApiResponse<object>(false, "该邮箱已存在", null));

        var candidate = new Candidate
        {
            Name = request.FullName ?? request.Email.Split('@')[0],
            Email = request.Email,
            Phone = request.Phone ?? "",
            CurrentPosition = request.CurrentPosition ?? "",
            CurrentCompany = request.CurrentCompany ?? "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Candidates.Add(candidate);
        await _dbContext.SaveChangesAsync();

        return Created($"/api/candidates/{candidate.Id}", new ApiResponse<object>(true, "创建成功", new { candidate.Id, candidate.Name }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> HardDeleteCandidate(long id)
    {
        var candidate = await _dbContext.Candidates.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (candidate == null)
        {
            return NotFound(new ApiResponse<object>(false, "候选人不存在", null));
        }

        _dbContext.Candidates.Remove(candidate);
        await _dbContext.SaveChangesAsync();

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "HARD_DELETE", "Candidate", id, $"彻底删除候选人 {candidate.Name}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "删除成功", null));
    }

    [HttpGet("export/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> ExportCandidateData(long id)
    {
        var candidate = await _dbContext.Candidates.FindAsync(id);
        if (candidate == null)
        {
            return NotFound(new ApiResponse<object>(false, "候选人不存在", null));
        }

        var hasConsent = await _consentService.HasActiveConsent(id);
        if (!hasConsent)
        {
            return BadRequest(new ApiResponse<object>(false, "候选人数据导出需要有效的同意记录", null));
        }

        var exportData = new
        {
            candidate.Id,
            candidate.Name,
            candidate.Email,
            candidate.Phone,
            candidate.Gender,
            candidate.Age,
            candidate.Education,
            candidate.CurrentPosition,
            candidate.CurrentCompany,
            candidate.WorkExperience,
            candidate.ExpectedSalary,
            candidate.ResumeUrl,
            candidate.Source,
            candidate.Remark,
            ExportTime = DateTime.UtcNow
        };

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "EXPORT", "Candidate", id, $"导出候选人 {candidate.Name} 的数据", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "导出成功", exportData));
    }

    [HttpPost("{id}/consent")]
    public async Task<ActionResult<ApiResponse<object>>> RecordConsent(long id, [FromBody] RecordConsentRequest request)
    {
        var candidate = await _dbContext.Candidates.FindAsync(id);
        if (candidate == null)
        {
            return NotFound(new ApiResponse<object>(false, "候选人不存在", null));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var consent = await _consentService.RecordConsent(id, request.ConsentType, ipAddress);

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "CONSENT", "Candidate", id, $"为候选人 {candidate.Name} 记录 {request.ConsentType} 同意", HttpContext);
        }

        var result = new
        {
            consent.Id,
            consent.CandidateId,
            consent.ConsentType,
            consent.ConsentDate,
            consent.IsActive
        };

        return Ok(new ApiResponse<object>(true, "记录成功", result));
    }

    [HttpDelete("{id}/consent")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeConsent(long id)
    {
        var candidate = await _dbContext.Candidates.FindAsync(id);
        if (candidate == null)
        {
            return NotFound(new ApiResponse<object>(false, "候选人不存在", null));
        }

        var success = await _consentService.RevokeConsent(id);
        if (!success)
        {
            return BadRequest(new ApiResponse<object>(false, "撤销失败，可能没有有效的同意记录", null));
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "REVOKE_CONSENT", "Candidate", id, $"撤销候选人 {candidate.Name} 的同意", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "撤销成功", null));
    }

    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

public record RecordConsentRequest(string ConsentType);
public record CreateCandidateRequest(string? FullName, string Email, string? Phone, string? CurrentPosition, string? CurrentCompany);