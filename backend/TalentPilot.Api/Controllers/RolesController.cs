using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly RoleService _roleService;
    private readonly OperationLogService _logService;

    public RolesController(RoleService roleService, OperationLogService logService)
    {
        _roleService = roleService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetRoles()
    {
        var roles = await _roleService.GetAllRoles();
        var result = roles.Select(r => new
        {
            r.Id,
            r.RoleName,
            r.RoleKey,
            r.Description,
            r.IsSystem,
            r.CreatedAt,
            r.UpdatedAt
        });
        return Ok(new ApiResponse<object>(true, "获取成功", result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetRole(long id)
    {
        var role = await _roleService.GetRoleById(id);
        if (role == null)
        {
            return NotFound(new ApiResponse<object>(false, "角色不存在", null));
        }

        var result = new
        {
            role.Id,
            role.RoleName,
            role.RoleKey,
            role.Description,
            role.IsSystem,
            role.CreatedAt,
            role.UpdatedAt
        };
        return Ok(new ApiResponse<object>(true, "获取成功", result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName) || string.IsNullOrWhiteSpace(request.RoleKey))
        {
            return BadRequest(new ApiResponse<object>(false, "角色名和角色Key不能为空", null));
        }

        var role = await _roleService.CreateRole(request.RoleName, request.RoleKey, request.Description);
        if (role == null)
        {
            return BadRequest(new ApiResponse<object>(false, "角色Key已存在", null));
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "CREATE", "Role", role.Id, $"创建角色 {role.RoleName}", HttpContext);
        }

        var result = new
        {
            role.Id,
            role.RoleName,
            role.RoleKey,
            role.Description,
            role.IsSystem
        };
        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, new ApiResponse<object>(true, "创建成功", result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateRole(long id, [FromBody] UpdateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
        {
            return BadRequest(new ApiResponse<object>(false, "角色名不能为空", null));
        }

        var role = await _roleService.UpdateRole(id, request.RoleName, request.Description);
        if (role == null)
        {
            return NotFound(new ApiResponse<object>(false, "角色不存在", null));
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "UPDATE", "Role", role.Id, $"更新角色 {role.RoleName}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "更新成功", null));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRole(long id)
    {
        var role = await _roleService.GetRoleById(id);
        if (role == null)
        {
            return NotFound(new ApiResponse<object>(false, "角色不存在", null));
        }

        if (role.IsSystem)
        {
            return BadRequest(new ApiResponse<object>(false, "预置角色不可删除", null));
        }

        var success = await _roleService.DeleteRole(id);
        if (!success)
        {
            return BadRequest(new ApiResponse<object>(false, "删除失败", null));
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "DELETE", "Role", id, $"删除角色 {role.RoleName}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "删除成功", null));
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<object>>> GetRolePermissions(long id)
    {
        var role = await _roleService.GetRoleById(id);
        if (role == null)
        {
            return NotFound(new ApiResponse<object>(false, "角色不存在", null));
        }

        var permissions = await _roleService.GetRolePermissions(id);
        return Ok(new ApiResponse<object>(true, "获取成功", permissions));
    }

    [HttpPut("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateRolePermissions(long id, [FromBody] UpdateRolePermissionsRequest request)
    {
        var role = await _roleService.GetRoleById(id);
        if (role == null)
        {
            return NotFound(new ApiResponse<object>(false, "角色不存在", null));
        }

        var success = await _roleService.UpdateRolePermissions(id, request.PermissionKeys);
        if (!success)
        {
            return BadRequest(new ApiResponse<object>(false, "更新权限失败", null));
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "UPDATE_PERMISSIONS", "Role", id, $"更新角色 {role.RoleName} 的权限", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "更新成功", null));
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

public record CreateRoleRequest(string RoleName, string RoleKey, string? Description);
public record UpdateRoleRequest(string RoleName, string? Description);
public record UpdateRolePermissionsRequest(List<string> PermissionKeys);