using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/job-channel-contents")]
[ProducesResponseType(typeof(ApiResponse<JobChannelContentDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<List<JobChannelContentDto>>), 200)]
[ProducesResponseType(typeof(ApiResponse<AdaptContentResponse>), 200)]
public class JobChannelContentsController : ControllerBase
{
    private readonly ContentAdaptationService _contentAdaptationService;
    private readonly ILogger<JobChannelContentsController> _logger;

    public JobChannelContentsController(
        ContentAdaptationService contentAdaptationService,
        ILogger<JobChannelContentsController> logger)
    {
        _contentAdaptationService = contentAdaptationService;
        _logger = logger;
    }

    /// <summary>获取职位所有渠道适配内容</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllByJobPostId([FromQuery] int jobPostId)
    {
        var results = await _contentAdaptationService.GetAllByJobPostIdAsync(jobPostId);
        return Ok(new ApiResponse<List<JobChannelContentDto>>(true, "获取成功", results));
    }

    /// <summary>按渠道类型获取适配内容</summary>
    [HttpGet("{channelType}")]
    public async Task<IActionResult> GetByChannelType([FromQuery] int jobPostId, string channelType)
    {
        var result = await _contentAdaptationService.GetByChannelTypeAsync(jobPostId, channelType);
        if (result == null)
            return NotFound(new ApiResponse<JobChannelContentDto>(false, $"No content found for channel {channelType}", null));

        return Ok(new ApiResponse<JobChannelContentDto>(true, "获取成功", result));
    }

    /// <summary>触发职位内容适配到各渠道</summary>
    [HttpPost("adapt")]
    public async Task<IActionResult> AdaptContent([FromBody] AdaptContentRequest request)
    {
        if (request.JobPostId <= 0)
            return BadRequest(new ApiResponse<AdaptContentResponse>(false, "Invalid JobPostId", null));

        if (request.ChannelTypes == null || request.ChannelTypes.Count == 0)
            return BadRequest(new ApiResponse<AdaptContentResponse>(false, "At least one channel type is required", null));

        var username = User.Identity?.Name ?? "system";

        try
        {
            var results = await _contentAdaptationService.AdaptJobPostAsync(request.JobPostId, request.ChannelTypes, username);

            var response = new AdaptContentResponse
            {
                Success = true,
                Message = $"Successfully adapted to {results.Count} channel(s)",
                Results = results
            };

            return Ok(new ApiResponse<AdaptContentResponse>(true, "适配成功", response));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<AdaptContentResponse>(false, ex.Message, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adapt content for job post {JobPostId}", request.JobPostId);
            return StatusCode(500, new ApiResponse<AdaptContentResponse>(false, "Internal server error: " + ex.Message, null));
        }
    }

    /// <summary>更新适配内容（手动编辑）</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContentRequest request)
    {
        var result = await _contentAdaptationService.UpdateAsync(id, request);
        if (result == null)
            return NotFound(new ApiResponse<JobChannelContentDto>(false, $"Content with id {id} not found", null));

        return Ok(new ApiResponse<JobChannelContentDto>(true, "更新成功", result));
    }

    /// <summary>删除适配内容</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _contentAdaptationService.DeleteAsync(id);
        if (!success)
            return NotFound(new ApiResponse<bool>(false, $"Content with id {id} not found", false));

        return Ok(new ApiResponse<bool>(true, "Deleted successfully", true));
    }
}