using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// Matches
/// </summary>
[ApiController]
[Route("api/matches")]
[Authorize(Roles = "admin,hr")]
public class MatchesController : ControllerBase
{
    private readonly MatchingService _matchingService;

    public MatchesController(MatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> GetMatches(
        [FromQuery] int? jobPostId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _matchingService.GetMatchResultsAsync(jobPostId, page, pageSize);

        return Ok(new ApiResponse<object>(true, "获取成功", new
        {
            total,
            page,
            pageSize,
            items = items.Select(m => new
            {
                m.Id,
                m.ResumeId,
                m.JobPostId,
                m.Score,
                m.MatchedSkills,
                m.MissingSkills,
                m.Summary,
                m.Status,
                m.CreatedAt
            })
        }));
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<object>>> CalculateMatch([FromBody] CalculateMatchRequest request)
    {
        var result = await _matchingService.CalculateMatchAsync(request.ResumeId, request.JobPostId);
        if (result == null)
            return NotFound(new ApiResponse<object>(false, "简历或职位不存在", null));

        return Ok(new ApiResponse<object>(true, "匹配计算成功", new
        {
            result.Id,
            result.ResumeId,
            result.JobPostId,
            result.Score,
            result.MatchedSkills,
            result.MissingSkills,
            result.Summary,
            result.Status
        }));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMatchStatus(int id, [FromBody] UpdateMatchStatusRequest request)
    {
        var validStatuses = new[] { "Pending", "Reviewed", "Accepted", "Rejected" };
        if (!validStatuses.Contains(request.Status))
            return BadRequest(new ApiResponse<object>(false, "无效的状态", null));

        var result = await _matchingService.UpdateMatchStatusAsync(id, request.Status);
        if (result == null)
            return NotFound(new ApiResponse<object>(false, "匹配结果不存在", null));

        return Ok(new ApiResponse<object>(true, "状态更新成功", new { result.Id, result.Status }));
    }

    [HttpPatch("{id}/threshold")]
    public async Task<ActionResult<ApiResponse<object>>> OverrideMatchThreshold(int id, [FromBody] OverrideThresholdRequest request)
    {
        var match = await _matchingService.UpdateMatchThresholdAsync(id, request.OverrideThreshold);
        if (match == null)
            return NotFound(new ApiResponse<object>(false, "匹配结果不存在", null));

        return Ok(new ApiResponse<object>(true, "阈值更新成功", new { match.Id, match.MatchThreshold }));
    }
}

public class CalculateMatchRequest
{
    public int ResumeId { get; set; }
    public int JobPostId { get; set; }
}

public class UpdateMatchStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class OverrideThresholdRequest
{
    public decimal? OverrideThreshold { get; set; }
}
