using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/channel-credentials")]
[Authorize(Roles = "admin")]
public class ChannelCredentialsController : ControllerBase
{
    private readonly ChannelCredentialService _service;

    public ChannelCredentialsController(ChannelCredentialService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAll()
    {
        var creds = await _service.GetAllAsync();
        return Ok(new ApiResponse<object>(true, "获取成功", new { items = creds }));
    }

    [HttpGet("{channelType}")]
    public async Task<ActionResult<ApiResponse<object>>> GetByType(string channelType)
    {
        var cred = await _service.GetByChannelTypeAsync(channelType);
        if (cred == null)
            return NotFound(new ApiResponse<object>(false, "渠道凭证不存在", null));

        return Ok(new ApiResponse<object>(true, "获取成功", cred));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateChannelCredentialRequest request)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
        var (cred, error) = await _service.CreateAsync(request, createdBy);

        if (error != null)
            return BadRequest(new ApiResponse<object>(false, error, null));

        return Created($"/api/channel-credentials/{cred!.ChannelType}",
            new ApiResponse<object>(true, "创建成功", new { cred.ChannelType, cred.ChannelName }));
    }

    [HttpPut("{channelType}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(string channelType, [FromBody] UpdateChannelCredentialRequest request)
    {
        var (cred, error) = await _service.UpdateAsync(channelType, request);

        if (error != null)
            return BadRequest(new ApiResponse<object>(false, error, null));

        return Ok(new ApiResponse<object>(true, "更新成功", new { cred!.ChannelType }));
    }

    [HttpDelete("{channelType}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string channelType)
    {
        var success = await _service.DeleteAsync(channelType);
        if (!success)
            return NotFound(new ApiResponse<object>(false, "渠道凭证不存在", null));

        return Ok(new ApiResponse<object>(true, "删除成功", null));
    }

    [HttpPost("{channelType}/validate")]
    public async Task<ActionResult<ApiResponse<object>>> Validate(string channelType)
    {
        var (success, message) = await _service.ValidateAsync(channelType);

        return Ok(new ApiResponse<object>(success, message, null));
    }
}
