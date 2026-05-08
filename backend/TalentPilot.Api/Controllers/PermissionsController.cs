using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    private readonly PermissionService _permissionService;

    public PermissionsController(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    public ActionResult<ApiResponse<object>> GetAllPermissions()
    {
        var permissions = _permissionService.GetAllPermissions();
        var result = permissions.Select(kvp => new
        {
            Module = kvp.Key,
            Permissions = kvp.Value
        });
        return Ok(new ApiResponse<object>(true, "获取成功", result));
    }

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<object>>> GetUserPermissions(long userId)
    {
        var permissions = await _permissionService.GetUserPermissions(userId);
        return Ok(new ApiResponse<object>(true, "获取成功", permissions));
    }

    [HttpGet("menu")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetMenuTree()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ApiResponse<object>(false, "未登录", null));
        }

        var permissions = await _permissionService.GetUserPermissions(userId.Value);
        var menuTree = _permissionService.GetMenuTree(permissions);
        return Ok(new ApiResponse<object>(true, "获取成功", menuTree));
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