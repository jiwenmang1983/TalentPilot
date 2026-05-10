using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// Resumes
/// </summary>
[ApiController]
[Route("api/resumes")]
[Authorize(Roles = "admin,hr")]
public class ResumesController : ControllerBase
{
    private readonly ResumeCollectionService _resumeService;
    private readonly ResumeParsingService _parsingService;
    private readonly ILogger<ResumesController> _logger;

    public ResumesController(
        ResumeCollectionService resumeService,
        ResumeParsingService parsingService,
        ILogger<ResumesController> logger)
    {
        _resumeService = resumeService;
        _parsingService = parsingService;
        _logger = logger;
    }

    [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> GetResumes(
        [FromQuery] int? jobPostId = null,
        [FromQuery] string? channel = null,
        [FromQuery] decimal? minScore = null,
        [FromQuery] decimal? maxScore = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _resumeService.ListResumesWithMatchAsync(jobPostId, channel, minScore, maxScore, page, pageSize);

        return Ok(new ApiResponse<object>(true, "获取成功", new
        {
            total,
            page,
            pageSize,
            items
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetResume(int id)
    {
        var resume = await _resumeService.GetResumeByIdAsync(id);
        if (resume == null)
            return NotFound(new ApiResponse<object>(false, "简历不存在", null));

        return Ok(new ApiResponse<object>(true, "获取成功", resume));
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ApiResponse<object>>> UploadResume([FromBody] UploadResumeRequest request)
    {
        var resume = await _resumeService.CreateResumeAsync(
            request.CandidateName,
            request.Phone,
            request.Email,
            "Manual",
            request.RawFilePath,
            request.SourceJobPostId
        );

        return Ok(new ApiResponse<object>(true, "上传成功", new
        {
            resume.Id,
            resume.CandidateName,
            resume.ParsedStatus
        }));
    }

    [HttpPost("mock-collect")]
    public async Task<ActionResult<ApiResponse<object>>> MockCollectResumes([FromBody] MockCollectRequest request)
    {
        var resumes = await _resumeService.MockCollectResumesAsync(request.Channel, request.Count);

        return Ok(new ApiResponse<object>(true, "模拟采集成功", new
        {
            count = resumes.Count,
            resumes = resumes.Select(r => new
            {
                r.Id,
                r.CandidateName,
                r.Phone,
                r.Email,
                r.Source
            })
        }));
    }

    [HttpPost("{id}/parse")]
    public async Task<ActionResult<ApiResponse<object>>> ParseResume(int id)
    {
        try
        {
            var result = await _parsingService.ParseResumeAsync(id);
            if (result == null)
            {
                return NotFound(new ApiResponse<object>(false, "简历不存在", null));
            }

            return Ok(new ApiResponse<object>(true, "解析成功", result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<object>(false, $"解析失败: {ex.Message}", null));
        }
    }

    [HttpPost("parse")]
    public async Task<ActionResult<ApiResponse<object>>> ParseResumeText([FromBody] ResumeParseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResumeText))
        {
            return BadRequest(new ApiResponse<object>(false, "简历文本不能为空", null));
        }

        try
        {
            var result = await _parsingService.ParseResumeTextAsync(request.ResumeText);
            if (result == null)
            {
                return BadRequest(new ApiResponse<object>(false, "解析失败，请稍后重试", null));
            }

            return Ok(new ApiResponse<object>(true, "解析成功", result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse resume text");
            return BadRequest(new ApiResponse<object>(false, $"解析失败: {ex.Message}", null));
        }
    }

    /// <summary>
    /// 触发立即采集
    /// </summary>
    [HttpPost("collect-now")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> CollectNow([FromBody] CollectNowRequest? request = null)
    {
        await _resumeService.TriggerImmediateCollectionAsync(request?.JobPostId);
        return Ok(new ApiResponse<object>(true, "采集任务已触发", null));
    }

    /// <summary>
    /// 获取简历来源列表
    /// </summary>
    [HttpGet("sources")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetSources()
    {
        var sources = await _resumeService.GetSourcesAsync();
        return Ok(new ApiResponse<object>(true, "获取成功", sources.Select(s => new
        {
            s.Id,
            s.Channel,
            s.IsActive,
            s.LastSyncAt,
            s.CreatedAt
        })));
    }
}

public class UploadResumeRequest
{
    public string? CandidateName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? RawFilePath { get; set; }
    public int? SourceJobPostId { get; set; }
}

public class MockCollectRequest
{
    public string Channel { get; set; } = "Boss";
    public int Count { get; set; } = 5;
}

public class CollectNowRequest
{
    public int? JobPostId { get; set; }
}
