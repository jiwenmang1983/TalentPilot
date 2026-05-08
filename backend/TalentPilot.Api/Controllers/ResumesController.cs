using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize(Roles = "admin,hr")]
public class ResumesController : ControllerBase
{
    private readonly ResumeCollectionService _resumeService;
    private readonly ResumeParsingService _parsingService;

    public ResumesController(
        ResumeCollectionService resumeService,
        ResumeParsingService parsingService)
    {
        _resumeService = resumeService;
        _parsingService = parsingService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetResumes(
        [FromQuery] string? source = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _resumeService.ListResumesAsync(source, page, pageSize);

        return Ok(new ApiResponse<object>(true, "获取成功", new
        {
            total,
            page,
            pageSize,
            items = items.Select(r => new
            {
                r.Id,
                r.CandidateName,
                r.Phone,
                r.Email,
                r.Source,
                r.ParsedStatus,
                r.CandidateId,
                r.CreatedAt
            })
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
