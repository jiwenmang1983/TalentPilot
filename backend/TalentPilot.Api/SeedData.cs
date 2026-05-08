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

        // Create seed candidates
        var candidate1 = new Candidate
        {
            Name = "张三",
            Email = "zhangsan@example.com",
            Phone = "13800138001",
            Gender = "男",
            Age = 28,
            Education = "本科",
            CurrentPosition = "软件工程师",
            CurrentCompany = "某科技公司",
            WorkExperience = 5,
            ExpectedSalary = 30000,
            Skills = "C#, JavaScript, Vue.js",
            Source = "招聘网站",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Candidates.Add(candidate1);

        var candidate2 = new Candidate
        {
            Name = "李四",
            Email = "lisi@example.com",
            Phone = "13800138002",
            Gender = "女",
            Age = 26,
            Education = "硕士",
            CurrentPosition = "产品经理",
            CurrentCompany = "某互联网公司",
            WorkExperience = 3,
            ExpectedSalary = 35000,
            Skills = "产品设计, 需求分析, 项目管理",
            Source = "内部推荐",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Candidates.Add(candidate2);

        var candidate3 = new Candidate
        {
            Name = "王五",
            Email = "wangwu@example.com",
            Phone = "13800138003",
            Gender = "男",
            Age = 30,
            Education = "博士",
            CurrentPosition = "技术总监",
            CurrentCompany = "某大型企业",
            WorkExperience = 8,
            ExpectedSalary = 50000,
            Skills = "架构设计, 技术管理, C++, Python",
            Source = "猎头",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Candidates.Add(candidate3);

        await context.SaveChangesAsync();

        // Create seed job posts
        var jobPost1 = new JobPost
        {
            Title = "高级前端工程师",
            Department = "技术部",
            Description = "负责公司前端架构设计与开发",
            Requirements = "5年以上前端开发经验，熟练掌握Vue.js或React",
            SalaryMin = 25000,
            SalaryMax = 45000,
            Experience = "5年以上",
            Education = "本科及以上",
            Status = "Published",
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.JobPosts.Add(jobPost1);

        var jobPost2 = new JobPost
        {
            Title = "产品经理",
            Department = "产品部",
            Description = "负责产品规划与设计",
            Requirements = "3年以上产品经验，有B端产品经验优先",
            SalaryMin = 20000,
            SalaryMax = 40000,
            Experience = "3年以上",
            Education = "本科及以上",
            Status = "Published",
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.JobPosts.Add(jobPost2);

        await context.SaveChangesAsync();

        // Create seed interview invitations
        var invitation1 = new InterviewInvitation
        {
            CandidateId = candidate1.Id,
            JobPostId = jobPost1.Id,
            InvitedByUserId = adminUser.Id,
            InterviewTime = DateTime.UtcNow.AddDays(1),
            TimeSlotStart = DateTime.UtcNow.AddDays(1).Date,
            TimeSlotEnd = DateTime.UtcNow.AddDays(1).Date.AddHours(23),
            Status = InterviewInvitationStatus.Confirmed.ToString(),
            InviteToken = Guid.NewGuid().ToString(),
            InviteSentAt = DateTime.UtcNow.AddDays(-1),
            ConfirmedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        context.InterviewInvitations.Add(invitation1);

        var invitation2 = new InterviewInvitation
        {
            CandidateId = candidate2.Id,
            JobPostId = jobPost2.Id,
            InvitedByUserId = adminUser.Id,
            InterviewTime = DateTime.UtcNow.AddDays(2),
            TimeSlotStart = DateTime.UtcNow.AddDays(2).Date,
            TimeSlotEnd = DateTime.UtcNow.AddDays(2).Date.AddHours(23),
            Status = InterviewInvitationStatus.PendingConfirmation.ToString(),
            InviteToken = Guid.NewGuid().ToString(),
            InviteSentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        context.InterviewInvitations.Add(invitation2);

        var invitation3 = new InterviewInvitation
        {
            CandidateId = candidate3.Id,
            JobPostId = jobPost1.Id,
            InvitedByUserId = adminUser.Id,
            InterviewTime = DateTime.UtcNow.AddDays(3),
            TimeSlotStart = DateTime.UtcNow.AddDays(3).Date,
            TimeSlotEnd = DateTime.UtcNow.AddDays(3).Date.AddHours(23),
            Status = InterviewInvitationStatus.Confirmed.ToString(),
            InviteToken = Guid.NewGuid().ToString(),
            InviteSentAt = DateTime.UtcNow.AddDays(-3),
            ConfirmedAt = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };
        context.InterviewInvitations.Add(invitation3);

        await context.SaveChangesAsync();

        // Create seed AI Interview Sessions
        var session1 = new AIInterviewSession
        {
            InterviewInvitationId = invitation1.Id,
            CandidateId = candidate1.Id,
            JobPostId = jobPost1.Id,
            SessionToken = Guid.NewGuid().ToString(),
            Status = AIInterviewSessionStatus.Completed.ToString(),
            StartTime = DateTime.UtcNow.AddDays(-1).AddHours(10),
            EndTime = DateTime.UtcNow.AddDays(-1).AddHours(11),
            DurationSeconds = 3600,
            OverallScore = "88",
            AiComments = "候选人表现优秀，具备扎实的前端技术能力和良好的沟通技巧。",
            RecordingUrl = $"https://mock-recording.talentpilot.com/session1",
            Transcript = "[{\"questionId\":\"Q1\",\"answer\":\"我是张三，5年前端开发经验...\",\"timestamp\":\"2026-05-07T10:00:00Z\"},{\"questionId\":\"Q2\",\"answer\":\"我应聘贵公司是因为...\",\"timestamp\":\"2026-05-07T10:02:00Z\"}]",
            CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(9),
            UpdatedAt = DateTime.UtcNow.AddDays(-1).AddHours(11)
        };
        context.AIInterviewSessions.Add(session1);

        var session2 = new AIInterviewSession
        {
            InterviewInvitationId = invitation3.Id,
            CandidateId = candidate3.Id,
            JobPostId = jobPost1.Id,
            SessionToken = Guid.NewGuid().ToString(),
            Status = AIInterviewSessionStatus.InProgress.ToString(),
            StartTime = DateTime.UtcNow.AddHours(-1),
            DurationSeconds = 1800,
            Transcript = "[{\"questionId\":\"Q1\",\"answer\":\"我是王五...\",\"timestamp\":\"2026-05-08T09:00:00Z\"},{\"questionId\":\"Q2\",\"answer\":\"我应聘这个职位...\",\"timestamp\":\"2026-05-08T09:02:00Z\"},{\"questionId\":\"Q3\",\"answer\":\"我的最大优势是...\",\"timestamp\":\"2026-05-08T09:05:00Z\"},{\"questionId\":\"Q4\",\"answer\":\"我通常这样处理压力...\",\"timestamp\":\"2026-05-08T09:07:00Z\"}]",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow
        };
        context.AIInterviewSessions.Add(session2);

        var session3 = new AIInterviewSession
        {
            InterviewInvitationId = invitation2.Id,
            CandidateId = candidate2.Id,
            JobPostId = jobPost2.Id,
            SessionToken = Guid.NewGuid().ToString(),
            Status = AIInterviewSessionStatus.Pending.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.AIInterviewSessions.Add(session3);

        await context.SaveChangesAsync();

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