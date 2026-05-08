using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public class PermissionService
{
    private readonly TalentPilotDbContext _dbContext;

    public static readonly List<string> AllPermissions = new()
    {
        "users:read", "users:create", "users:update", "users:delete",
        "roles:read", "roles:create", "roles:update", "roles:delete",
        "departments:read", "departments:create", "departments:update", "departments:delete",
        "job_channels:manage",
        "job_positions:manage",
        "candidates:read", "candidates:create", "candidates:update", "candidates:delete",
        "interviews:manage",
        "interview.sessions:read",
        "reports:view"
    };

    public static readonly Dictionary<string, List<string>> MenuModules = new()
    {
        ["用户管理"] = new() { "users:read", "users:create", "users:update", "users:delete" },
        ["角色管理"] = new() { "roles:read", "roles:create", "roles:update", "roles:delete" },
        ["部门管理"] = new() { "departments:read", "departments:create", "departments:update", "departments:delete" },
        ["职位管理"] = new() { "job_positions:manage" },
        ["渠道管理"] = new() { "job_channels:manage" },
        ["候选人"] = new() { "candidates:read", "candidates:create", "candidates:update", "candidates:delete" },
        ["面试管理"] = new() { "interviews:manage" },
        ["报表"] = new() { "reports:view" }
    };

    public PermissionService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Dictionary<string, List<string>> GetAllPermissions()
    {
        return MenuModules;
    }

    public async Task<List<string>> GetUserPermissions(long userId)
    {
        var user = await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.Role == null)
        {
            return new List<string>();
        }

        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId)
            .Select(rp => rp.PermissionKey)
            .ToListAsync();
    }

    public List<MenuNode> GetMenuTree(List<string> permissions)
    {
        var menuNodes = new List<MenuNode>();

        if (permissions.Any(p => p.StartsWith("users")))
        {
            menuNodes.Add(new MenuNode("users", "用户管理", "UserOutlined", "/users", new List<MenuNode>()));
        }

        if (permissions.Any(p => p.StartsWith("roles")))
        {
            menuNodes.Add(new MenuNode("roles", "角色管理", "SafetyOutlined", "/roles", new List<MenuNode>()));
        }

        if (permissions.Any(p => p.StartsWith("departments")))
        {
            menuNodes.Add(new MenuNode("departments", "部门管理", "TeamOutlined", "/departments", new List<MenuNode>()));
        }

        var recruitChildren = new List<MenuNode>();

        if (permissions.Any(p => p == "job_positions:manage"))
        {
            recruitChildren.Add(new MenuNode("positions", "职位管理", "SolutionOutlined", "/positions", new List<MenuNode>()));
        }

        if (permissions.Any(p => p == "job_channels:manage"))
        {
            recruitChildren.Add(new MenuNode("channels", "渠道管理", "AppstoreOutlined", "/channels", new List<MenuNode>()));
        }

        if (permissions.Any(p => p.StartsWith("candidates")))
        {
            recruitChildren.Add(new MenuNode("candidates", "候选人", "ContactsOutlined", "/candidates", new List<MenuNode>()));
        }

        if (permissions.Any(p => p == "interviews:manage"))
        {
            recruitChildren.Add(new MenuNode("interviews", "面试邀约", "ScheduleOutlined", "/recruitment/interviews", new List<MenuNode>()));
        }

        if (permissions.Any(p => p == "interview.sessions:read"))
        {
            recruitChildren.Add(new MenuNode("ai-interview-sessions", "AI面试会话", "VideoCameraOutlined", "/interview/sessions", new List<MenuNode>()));
        }

        if (recruitChildren.Count > 0)
        {
            menuNodes.Add(new MenuNode("recruit", "招聘管理", "ProjectOutlined", "", recruitChildren));
        }

        if (permissions.Any(p => p == "reports:view"))
        {
            menuNodes.Add(new MenuNode("reports", "数据报表", "BarChartOutlined", "/reports", new List<MenuNode>()));
        }

        return menuNodes;
    }
}

public record MenuNode(
    string Key,
    string Title,
    string Icon,
    string Path,
    List<MenuNode> Children
);