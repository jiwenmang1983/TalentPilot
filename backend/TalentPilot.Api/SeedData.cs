using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TalentPilotDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TalentPilotDbContext>>();

        try
        {
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database ensured/created successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database initialization skipped (may already exist or connection failed)");
            return;
        }

        if (await context.Roles.AnyAsync())
        {
            logger.LogInformation("Seed data already exists, skipping");
            return;
        }

        var adminRole = await CreateRoleIfNotExists(context, "管理员", "admin", "系统管理员", true);
        var hrRole = await CreateRoleIfNotExists(context, "HR", "hr", "人事专员", true);
        var hiringManagerRole = await CreateRoleIfNotExists(context, "用人经理", "hiring_manager", "部门负责人", true);
        var userRole = await CreateRoleIfNotExists(context, "普通用户", "user", "普通用户", false);

        var hqDept = await CreateDepartmentIfNotExists(context, "总公司", "hq", null, 1);
        var techDept = await CreateDepartmentIfNotExists(context, "技术部", "tech", hqDept.Id, 2);
        var productGroup = await CreateDepartmentIfNotExists(context, "产品组", "product", techDept.Id, 3);
        var frontendGroup = await CreateDepartmentIfNotExists(context, "前端组", "frontend", techDept.Id, 3);
        var backendGroup = await CreateDepartmentIfNotExists(context, "后端组", "backend", techDept.Id, 3);
        var hrDept = await CreateDepartmentIfNotExists(context, "人力资源部", "hr_dept", hqDept.Id, 2);

        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@talentpilot.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TalentPilot2026"),
            FullName = "系统管理员",
            DepartmentId = hqDept.Id,
            RoleId = adminRole.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);

        var hrUser = new User
        {
            Username = "hr_admin",
            Email = "hr@talentpilot.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Hr@2026"),
            FullName = "人事主管",
            DepartmentId = hrDept.Id,
            RoleId = hrRole.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(hrUser);

        await context.SaveChangesAsync();

        await AssignPermissionsToRole(context, adminRole.Id, PermissionService.AllPermissions);

        var hrPermissions = new List<string>
        {
            "users:read", "users:create", "users:update",
            "roles:read",
            "departments:read", "departments:create", "departments:update",
            "candidates:read", "candidates:create", "candidates:update",
            "interviews:manage",
            "reports:view"
        };
        await AssignPermissionsToRole(context, hrRole.Id, hrPermissions);

        var hiringManagerPermissions = new List<string>
        {
            "candidates:read",
            "interviews:manage",
            "reports:view"
        };
        await AssignPermissionsToRole(context, hiringManagerRole.Id, hiringManagerPermissions);

        logger.LogInformation("Seed data created successfully. Admin: admin/TalentPilot2026, HR: hr_admin/Hr@2026");
    }

    private static async Task<Role> CreateRoleIfNotExists(TalentPilotDbContext context, string roleName, string roleKey, string description, bool isSystem)
    {
        var existing = await context.Roles.FirstOrDefaultAsync(r => r.RoleKey == roleKey);
        if (existing != null) return existing;

        var role = new Role
        {
            RoleName = roleName,
            RoleKey = roleKey,
            Description = description,
            IsSystem = isSystem,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    private static async Task<Department> CreateDepartmentIfNotExists(TalentPilotDbContext context, string name, string key, long? parentId, int level)
    {
        var existing = await context.Departments.FirstOrDefaultAsync(d => d.DepartmentKey == key);
        if (existing != null) return existing;

        var dept = new Department
        {
            DepartmentName = name,
            DepartmentKey = key,
            ParentId = parentId,
            Level = level,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();
        return dept;
    }

    private static async Task AssignPermissionsToRole(TalentPilotDbContext context, long roleId, List<string> permissions)
    {
        foreach (var permissionKey in permissions)
        {
            var exists = await context.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionKey == permissionKey);
            if (!exists)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionKey = permissionKey,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();
    }
}