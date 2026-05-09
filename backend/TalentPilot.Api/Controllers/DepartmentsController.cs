using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// Departments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class DepartmentsController : ControllerBase
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly DepartmentService _departmentService;
    private readonly OperationLogService _logService;

    public DepartmentsController(TalentPilotDbContext dbContext, DepartmentService departmentService, OperationLogService logService)
    {
        _dbContext = dbContext;
        _departmentService = departmentService;
        _logService = logService;
    }

    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<object>>> GetDepartmentTree()
    {
        var tree = await _departmentService.GetDepartmentTree();
        return Ok(new ApiResponse<object>(true, "获取成功", tree));
    }

    [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> GetAllDepartments()
    {
        var departments = await _dbContext.Departments
            .OrderBy(d => d.Level)
            .ThenBy(d => d.SortOrder)
            .Select(d => new {
                d.Id,
                d.DepartmentName,
                d.DepartmentKey,
                d.ParentId,
                d.Level,
                d.SortOrder,
                d.CreatedAt,
                d.UpdatedAt
            })
            .ToListAsync();
        return Ok(new ApiResponse<object>(true, "获取成功", departments));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetDepartment(long id)
    {
        var department = await _departmentService.GetDepartmentById(id);
        if (department == null)
        {
            return NotFound(new ApiResponse<object>(false, "部门不存在", null));
        }

        var result = new
        {
            department.Id,
            department.DepartmentName,
            department.DepartmentKey,
            department.ParentId,
            department.Level,
            department.SortOrder,
            department.CreatedAt,
            department.UpdatedAt
        };
        return Ok(new ApiResponse<object>(true, "获取成功", result));
    }

    [HttpGet("{id}/children")]
    public async Task<ActionResult<ApiResponse<object>>> GetSubDepartments(long id)
    {
        var department = await _departmentService.GetDepartmentById(id);
        if (department == null)
        {
            return NotFound(new ApiResponse<object>(false, "部门不存在", null));
        }

        var children = await _departmentService.GetSubDepartments(id);
        var result = children.Select(d => new
        {
            d.Id,
            d.DepartmentName,
            d.DepartmentKey,
            d.ParentId,
            d.Level,
            d.SortOrder,
            d.CreatedAt,
            d.UpdatedAt
        });
        return Ok(new ApiResponse<object>(true, "获取成功", result));
    }

    [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DepartmentName) || string.IsNullOrWhiteSpace(request.DepartmentKey))
        {
            return BadRequest(new ApiResponse<object>(false, "部门名称和部门Key不能为空", null));
        }

        var department = await _departmentService.CreateDepartment(
            request.DepartmentName,
            request.DepartmentKey,
            request.ParentId,
            request.SortOrder);

        if (department == null)
        {
            return BadRequest(new ApiResponse<object>(false, "部门Key已存在", null));
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "CREATE", "Department", department.Id, $"创建部门 {department.DepartmentName}", HttpContext);
        }

        var result = new
        {
            department.Id,
            department.DepartmentName,
            department.DepartmentKey,
            department.ParentId,
            department.Level,
            department.SortOrder
        };
        return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, new ApiResponse<object>(true, "创建成功", result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateDepartment(long id, [FromBody] UpdateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DepartmentName))
        {
            return BadRequest(new ApiResponse<object>(false, "部门名称不能为空", null));
        }

        var department = await _departmentService.UpdateDepartment(id, request.DepartmentName, request.SortOrder);
        if (department == null)
        {
            return NotFound(new ApiResponse<object>(false, "部门不存在", null));
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "UPDATE", "Department", department.Id, $"更新部门 {department.DepartmentName}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "更新成功", null));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDepartment(long id)
    {
        var department = await _departmentService.GetDepartmentById(id);
        if (department == null)
        {
            return NotFound(new ApiResponse<object>(false, "部门不存在", null));
        }

        var children = await _departmentService.GetSubDepartments(id);
        if (children.Any())
        {
            return BadRequest(new ApiResponse<object>(false, "请先删除子部门", null));
        }

        var success = await _departmentService.DeleteDepartment(id);
        if (!success)
        {
            return BadRequest(new ApiResponse<object>(false, "该部门存在子部门或用户，无法删除", null));
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "DELETE", "Department", id, $"删除部门 {department.DepartmentName}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "删除成功", null));
    }

    [HttpPut("{id}/move")]
    public async Task<ActionResult<ApiResponse<object>>> MoveDepartment(long id, [FromBody] MoveDepartmentRequest request)
    {
        var department = await _departmentService.GetDepartmentById(id);
        if (department == null)
        {
            return NotFound(new ApiResponse<object>(false, "部门不存在", null));
        }

        var success = await _departmentService.MoveDepartment(id, request.NewParentId);
        if (!success)
        {
            return BadRequest(new ApiResponse<object>(false, "移动失败（可能形成循环依赖）", null));
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            await _logService.RecordLog(currentUserId.Value, "MOVE", "Department", id, $"移动部门 {department.DepartmentName}", HttpContext);
        }

        return Ok(new ApiResponse<object>(true, "移动成功", null));
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

public record CreateDepartmentRequest(string DepartmentName, string DepartmentKey, long? ParentId, int SortOrder);
public record UpdateDepartmentRequest(string DepartmentName, int SortOrder);
public record MoveDepartmentRequest(long NewParentId);