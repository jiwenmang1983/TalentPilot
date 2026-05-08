using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TalentPilotDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TalentPilotDbContext>>();

        // Apply migrations or ensure database is created
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

        // Check if data already exists
        if (await context.Roles.AnyAsync())
        {
            logger.LogInformation("Seed data already exists, skipping");
            return;
        }

        // Create Admin role
        var adminRole = new Role
        {
            RoleName = "Administrator",
            RoleKey = "Admin",
            Description = "System Administrator",
            IsSystem = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Roles.Add(adminRole);

        // Create User role
        var userRole = new Role
        {
            RoleName = "User",
            RoleKey = "User",
            Description = "Regular User",
            IsSystem = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Roles.Add(userRole);

        await context.SaveChangesAsync();

        // Create default department
        var defaultDept = new Department
        {
            DepartmentName = "Headquarters",
            DepartmentKey = "HQ",
            Level = 1,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Departments.Add(defaultDept);
        await context.SaveChangesAsync();

        // Create admin user
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@talentpilot.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TalentPilot2026"),
            FullName = "System Administrator",
            DepartmentId = defaultDept.Id,
            RoleId = adminRole.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        logger.LogInformation("Seed data created successfully. Admin user: admin / TalentPilot2026");
    }
}
