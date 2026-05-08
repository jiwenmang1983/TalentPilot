using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var (success, message, response) = await _authService.Login(request, HttpContext);

        if (!success)
        {
            return Unauthorized(new ApiResponse<LoginResponse>(false, message, null));
        }

        return Ok(new ApiResponse<LoginResponse>(true, message, response));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var (success, message, response) = await _authService.Refresh(request);

        if (!success)
        {
            return Unauthorized(new ApiResponse<LoginResponse>(false, message, null));
        }

        return Ok(new ApiResponse<LoginResponse>(true, message, response));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiResponse<object>(false, "无效的用户标识", null));
        }

        await _authService.Logout(userId, HttpContext);
        return Ok(new ApiResponse<object>(true, "登出成功", null));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiResponse<object>(false, "无效的用户标识", null));
        }

        var user = await _authService.GetCurrentUser(userId);
        if (user == null)
        {
            return NotFound(new ApiResponse<object>(false, "用户不存在", null));
        }

        var userInfo = new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Phone,
            user.AvatarUrl,
            user.DepartmentId,
            DepartmentName = user.Department?.DepartmentName,
            user.RoleId,
            RoleName = user.Role?.RoleName,
            RoleKey = user.Role?.RoleKey,
            user.IsActive,
            user.LastLoginAt,
            user.CreatedAt
        };

        return Ok(new ApiResponse<object>(true, "获取成功", userInfo));
    }
}
