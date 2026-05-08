using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs.Auth;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public class DepartmentService
{
    private readonly TalentPilotDbContext _dbContext;

    public DepartmentService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DepartmentTreeNode>> GetDepartmentTree()
    {
        var allDepartments = await _dbContext.Departments
            .OrderBy(d => d.Level)
            .ThenBy(d => d.SortOrder)
            .ToListAsync();

        return BuildTree(allDepartments, null);
    }

    private List<DepartmentTreeNode> BuildTree(List<Department> departments, long? parentId)
    {
        return departments
            .Where(d => d.ParentId == parentId)
            .Select(d => new DepartmentTreeNode(
                d.Id,
                d.DepartmentName,
                d.DepartmentKey,
                d.ParentId,
                d.Level,
                d.SortOrder,
                BuildTree(departments, d.Id)
            ))
            .ToList();
    }

    public async Task<List<Department>> GetSubDepartments(long parentId)
    {
        return await _dbContext.Departments
            .Where(d => d.ParentId == parentId)
            .OrderBy(d => d.SortOrder)
            .ToListAsync();
    }

    public async Task<Department?> GetDepartmentById(long id)
    {
        return await _dbContext.Departments.FindAsync(id);
    }

    public async Task<Department?> CreateDepartment(string departmentName, string departmentKey, long? parentId, int sortOrder)
    {
        if (await _dbContext.Departments.AnyAsync(d => d.DepartmentKey == departmentKey))
        {
            return null;
        }

        int level = 1;
        if (parentId.HasValue)
        {
            var parent = await _dbContext.Departments.FindAsync(parentId.Value);
            if (parent != null)
            {
                level = parent.Level + 1;
            }
        }

        var department = new Department
        {
            DepartmentName = departmentName,
            DepartmentKey = departmentKey,
            ParentId = parentId,
            Level = level,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync();
        return department;
    }

    public async Task<Department?> UpdateDepartment(long id, string departmentName, int sortOrder)
    {
        var department = await _dbContext.Departments.FindAsync(id);
        if (department == null) return null;

        department.DepartmentName = departmentName;
        department.SortOrder = sortOrder;
        department.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return department;
    }

    public async Task<bool> DeleteDepartment(long id)
    {
        var department = await _dbContext.Departments.FindAsync(id);
        if (department == null) return false;

        var hasChildren = await _dbContext.Departments.AnyAsync(d => d.ParentId == id);
        if (hasChildren)
        {
            return false;
        }

        var hasUsers = await _dbContext.Users.AnyAsync(u => u.DepartmentId == id);
        if (hasUsers)
        {
            return false;
        }

        department.IsDeleted = true;
        department.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveDepartment(long id, long newParentId)
    {
        var department = await _dbContext.Departments.FindAsync(id);
        if (department == null) return false;

        if (id == newParentId)
        {
            return false;
        }

        if (await IsDescendant(id, newParentId))
        {
            return false;
        }

        var newParent = await _dbContext.Departments.FindAsync(newParentId);
        if (newParent == null) return false;

        department.ParentId = newParentId;
        department.Level = newParent.Level + 1;
        department.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await UpdateDescendantLevels(id);
        return true;
    }

    private async Task<bool> IsDescendant(long potentialDescendant, long potentialAncestor)
    {
        var current = await _dbContext.Departments.FindAsync(potentialDescendant);
        while (current?.ParentId != null)
        {
            if (current.ParentId == potentialAncestor)
            {
                return true;
            }
            current = await _dbContext.Departments.FindAsync(current.ParentId);
        }
        return false;
    }

    private async Task UpdateDescendantLevels(long parentId)
    {
        var children = await _dbContext.Departments.Where(d => d.ParentId == parentId).ToListAsync();
        foreach (var child in children)
        {
            var parent = await _dbContext.Departments.FindAsync(child.ParentId);
            if (parent != null)
            {
                child.Level = parent.Level + 1;
                await UpdateDescendantLevels(child.Id);
            }
        }
        await _dbContext.SaveChangesAsync();
    }
}

public record DepartmentTreeNode(
    long Id,
    string DepartmentName,
    string DepartmentKey,
    long? ParentId,
    int Level,
    int SortOrder,
    List<DepartmentTreeNode> Children
);