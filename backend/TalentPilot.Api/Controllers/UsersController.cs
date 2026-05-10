using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// Users
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class UsersController : ControllerBase
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly AuthService _authService;
    private readonly OperationLogService _logService;

    public UsersController(TalentPilotDbContext dbContext, AuthService authService, OperationLogService logService)
    {
        _dbContext = dbContext;
        _authService = authService;
        _logService = logService;
    }

    [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? roleId = null,
        [FromQuery] long? departmentId = null,
        [FromQuery] bool? isActive = null)
    {
        var query = _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .AsQueryable();

        if (roleId.HasValue)
            query = query.Where(u => u.RoleId == roleId.Value);

        if (departmentId.HasValue)
            query = query.Where(u => u.DepartmentId == departmentId.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItem(
                u.Id,
                u.Username,
                u.Email,
                u.FullName ?? "",
                u.Phone,
                u.AvatarUrl,
                u.DepartmentId,
                u.Department!.DepartmentName,
                u.RoleId,
                u.Role!.RoleName,
                u.IsActive,
                u.LastLoginAt,
                u.CreatedAt
            ))
            .ToListAsync();

        var result = new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = users
        };

        return Ok(new ApiResponse<object>(true, "获取成功", result));
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<object>>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(new ApiResponse<object>(false, "未登录", null));

        var user = await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
            return NotFound(new ApiResponse<object>(false, "用户不存在", null));

        return Ok(new ApiResponse<object>(true, "获取成功", new {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Phone,
            RoleKey = user.Role?.RoleKey,
            DepartmentId = user.DepartmentId
        }));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> GetUser(long id)
    {
        var user = await _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id);

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
            user.LastLoginIp,
            user.CreatedAt,
            user.UpdatedAt
        };

        return Ok(new ApiResponse<object>(true, "获取成功", userInfo));
    }

    [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(new ApiResponse<object>(false, "用户名已存在", null));
        }

        if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new ApiResponse<object>(false, "邮箱已被使用", null));
        }

        var role = await _dbContext.Roles.FindAsync(request.RoleId);
        if (role == null)
        {
            return BadRequest(new ApiResponse<object>(false, "角色不存在", null));
        }

        var department = await _dbContext.Departments.FindAsync(request.DepartmentId);
        if (department == null)
        {
            return BadRequest(new ApiResponse<object>(false, "部门不存在", null));
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            DepartmentId = request.DepartmentId,
            RoleId = request.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "CREATE", "User", user.Id, $"创建用户 {user.Username}", HttpContext);
        }

        var response = new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.DepartmentId,
            DepartmentName = department.DepartmentName,
            user.RoleId,
            RoleName = role.RoleName
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new ApiResponse<object>(true, "创建成功", response));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUser(long id, [FromBody] UpdateUserRequest request)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<object>(false, "用户不存在", null));
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
            {
                return BadRequest(new ApiResponse<object>(false, "邮箱已被使用", null));
            }
            user.Email = request.Email;
        }

        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrEmpty(request.Phone))
            user.Phone = request.Phone;

        if (request.DepartmentId.HasValue)
        {
            var dept = await _dbContext.Departments.FindAsync(request.DepartmentId.Value);
            if (dept == null)
            {
                return BadRequest(new ApiResponse<object>(false, "部门不存在", null));
            }
            user.DepartmentId = request.DepartmentId.Value;
        }

        if (request.RoleId.HasValue)
        {
            var role = await _dbContext.Roles.FindAsync(request.RoleId.Value);
            if (role == null)
            {
                return BadRequest(new ApiResponse<object>(false, "角色不存在", null));
            }
            user.RoleId = request.RoleId.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "UPDATE", "User", user.Id, $"更新用户 {user.Username}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "更新成功", null));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(long id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<object>(false, "用户不存在", null));
        }

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "DELETE", "User", user.Id, $"删除用户 {user.Username}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "删除成功", null));
    }

    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(long id, [FromBody] ResetPasswordRequest request)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<object>(false, "用户不存在", null));
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "RESET_PASSWORD", "User", user.Id, $"重置用户 {user.Username} 的密码", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "密码重置成功", null));
    }

    [HttpPost("{id}/toggle-active")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleActive(long id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<object>(false, "用户不存在", null));
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            var action = user.IsActive ? "启用" : "禁用";
            await _logService.RecordLog(currentUserId.Value, "TOGGLE_ACTIVE", "User", user.Id, $"{action}用户 {user.Username}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, $"用户{(user.IsActive ? "已启用" : "已禁用")}", new { user.IsActive }));
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
