using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public class RoleService
{
    private readonly TalentPilotDbContext _dbContext;

    public RoleService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Role>> GetAllRoles()
    {
        return await _dbContext.Roles
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.RoleName)
            .ToListAsync();
    }

    public async Task<Role?> GetRoleById(long id)
    {
        return await _dbContext.Roles.FindAsync(id);
    }

    public async Task<Role?> CreateRole(string roleName, string roleKey, string? description)
    {
        if (await _dbContext.Roles.AnyAsync(r => r.RoleKey == roleKey))
        {
            return null;
        }

        var role = new Role
        {
            RoleName = roleName,
            RoleKey = roleKey,
            Description = description,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();
        return role;
    }

    public async Task<Role?> UpdateRole(long id, string roleName, string? description)
    {
        var role = await _dbContext.Roles.FindAsync(id);
        if (role == null) return null;

        role.RoleName = roleName;
        role.Description = description;
        role.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return role;
    }

    public async Task<bool> DeleteRole(long id)
    {
        var role = await _dbContext.Roles.FindAsync(id);
        if (role == null) return false;

        if (role.IsSystem)
        {
            return false;
        }

        role.IsDeleted = true;
        role.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<string>> GetRolePermissions(long roleId)
    {
        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionKey)
            .ToListAsync();
    }

    public async Task<bool> UpdateRolePermissions(long roleId, List<string> permissionKeys)
    {
        var role = await _dbContext.Roles.FindAsync(roleId);
        if (role == null) return false;

        var existingPermissions = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        _dbContext.RolePermissions.RemoveRange(existingPermissions);

        foreach (var key in permissionKeys)
        {
            _dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionKey = key,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task AddPermissionToRole(long roleId, string permissionKey)
    {
        if (!await _dbContext.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionKey == permissionKey))
        {
            _dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionKey = permissionKey,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
        }
    }
}