using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// JobPosts
/// </summary>
[ApiController]
[Route("api/jobposts")]
[Authorize(Roles = "admin,hr")]
public class JobPostsController : ControllerBase
{
    private readonly JobPostService _jobPostService;
    private readonly MatchingService _matchingService;

    public JobPostsController(
        JobPostService jobPostService,
        MatchingService matchingService)
    {
        _jobPostService = jobPostService;
        _matchingService = matchingService;
    }

    [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> GetJobPosts(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _jobPostService.GetAllAsync(status, page, pageSize);

        return Ok(new ApiResponse<object>(true, "获取成功", new
        {
            total,
            page,
            pageSize,
            items = items.Select(j => new
            {
                j.Id,
                j.Title,
                j.Department,
                j.Description,
                j.Requirements,
                j.SalaryMin,
                j.SalaryMax,
                j.Experience,
                j.Education,
                j.Status,
                j.CreatedBy,
                j.CreatedAt,
                j.UpdatedAt
            })
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetJobPost(int id)
    {
        var jobPost = await _jobPostService.GetByIdAsync(id);
        if (jobPost == null)
            return NotFound(new ApiResponse<object>(false, "职位不存在", null));

        return Ok(new ApiResponse<object>(true, "获取成功", jobPost));
    }

    [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> CreateJobPost([FromBody] CreateJobPostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new ApiResponse<object>(false, "职位名称不能为空", null));

        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
        var jobPost = await _jobPostService.CreateAsync(request, createdBy);

        return Created($"/api/jobposts/{jobPost.Id}", new ApiResponse<object>(true, "创建成功", new
        {
            jobPost.Id,
            jobPost.Title,
            jobPost.Status
        }));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateJobPost(int id, [FromBody] UpdateJobPostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new ApiResponse<object>(false, "职位名称不能为空", null));

        var jobPost = await _jobPostService.UpdateAsync(id, request);
        if (jobPost == null)
            return NotFound(new ApiResponse<object>(false, "职位不存在", null));

        return Ok(new ApiResponse<object>(true, "更新成功", new { jobPost.Id, jobPost.Title }));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateJobPostStatus(int id, [FromBody] UpdateJobPostStatusRequest request)
    {
        var validStatuses = new[] { "Draft", "Published", "Paused", "Closed" };
        if (!validStatuses.Contains(request.Status))
            return BadRequest(new ApiResponse<object>(false, "无效的职位状态", null));

        var jobPost = await _jobPostService.UpdateStatusAsync(id, request.Status);
        if (jobPost == null)
            return NotFound(new ApiResponse<object>(false, "职位不存在", null));

        return Ok(new ApiResponse<object>(true, "状态更新成功", new { jobPost.Id, jobPost.Status }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteJobPost(int id)
    {
        var success = await _jobPostService.DeleteAsync(id);
        if (!success)
            return NotFound(new ApiResponse<object>(false, "职位不存在", null));

        return Ok(new ApiResponse<object>(true, "删除成功", null));
    }

    [HttpPost("{id}/match")]
    public async Task<ActionResult<ApiResponse<object>>> MatchJobPost(int id)
    {
        var jobPost = await _jobPostService.GetByIdAsync(id);
        if (jobPost == null)
            return NotFound(new ApiResponse<object>(false, "职位不存在", null));

        var results = await _matchingService.BatchMatchAsync(id);

        return Ok(new ApiResponse<object>(true, "批量匹配完成", new
        {
            count = results.Count,
            results = results.Select(r => new
            {
                r.Id,
                r.ResumeId,
                r.Score,
                r.Status
            })
        }));
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }

    public ApiResponse(bool success, string message, T? data)
    {
        Success = success;
        Message = message;
        Data = data;
    }
}
