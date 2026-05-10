# T-62: F-23 报告权限控制

## 需求来源
PRD §3.7 F-23

## 需求描述
报告查看权限隔离：
- HR/管理员：可查看所有报告
- 面试官（hiring_manager）：只能查看自己参与面试的候选人的报告
- 候选人：仅看自己的最终报告（基于session token）

## 现状
当前 `GetReports()` 返回全部报告，无权限过滤。所有角色都能看到所有报告。

## 实现方案

### 1. 修改 GetReports() - 添加行级权限过滤

在 `InterviewReportsController.GetReports()` 中，获取当前用户角色和ID，添加过滤逻辑：

```csharp
// 获取当前用户
var currentUserId = GetCurrentUserId();
var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

// HR和Admin：看全部报告（不过滤）
// hiring_manager：只看自己参与面试的报告
if (userRole == "hiring_manager")
{
    // 过滤：只返回该面试官参与过的AI面试Session对应的报告
    // 通过 AIInterviewSession.InterviewerUserId == currentUserId 关联
    query = query.Where(r => 
        _context.AIInterviewSessions
            .Any(s => s.Id == r.AIInterviewSessionId && s.InterviewerUserId == currentUserId)
    );
}
```

### 2. 修改 GetReport(id) - 单条报告权限检查

```csharp
// HR和Admin：允许
// hiring_manager：只能查看自己参与面试的报告，否则返回404
if (userRole == "hiring_manager")
{
    var hasAccess = await _context.AIInterviewSessions
        .AnyAsync(s => s.Id == report.AIInterviewSessionId && s.InterviewerUserId == currentUserId);
    if (!hasAccess)
        return NotFound(); // 隐藏不存在而非403（避免信息泄露）
}
```

### 3. AIInterviewSession 表确认有 InterviewerUserId 字段

检查 `AIInterviewSession` Entity 是否有 `InterviewerUserId` 字段。如果没有，改用其他方式关联（如 `AssignedInterviewerId` 或通过 `AIInterviewSessionParticipants` 表）。

### 3. 添加 InterviewerUserId 字段

当前 `AIInterviewSession` 没有 `InterviewerUserId` 字段。需要在 Entity 中添加：

```csharp
// AIInterviewSession.cs
public int? InterviewerUserId { get; set; }

[ForeignKey(nameof(InterviewerUserId))]
public virtual User? Interviewer { get; set; }
```

并在 MySQL 中执行建表语句：
```sql
ALTER TABLE AIInterviewSessions ADD COLUMN InterviewerUserId INT NULL;
ALTER TABLE AIInterviewSessions ADD CONSTRAINT FK_AIInterviewSessions_Users_InterviewerUserId 
  FOREIGN KEY (InterviewerUserId) REFERENCES Users(Id);
```

（或者让EF Core自动处理 migration，本项目手动建表，所以直接mysql执行）

### 4. 更新 StartSession 端点，记录当前用户为面试官

在 `AIInterviewSessionsController.StartSession()` 中，session开始时设置 `InterviewerUserId = currentUserId`。

### 5. 候选人查看自己报告

`GET /api/interview-reports/my` — 基于JWT中的candidateId，返回自己的报告。

## 验收标准
- [ ] HR/admin登录：看到全部报告列表
- [ ] hiring_manager登录：只看到自己参与面试的报告
- [ ] hiring_manager访问他人报告：返回404
- [ ] 候选人访问自己的报告（my端点）：可查看
- [ ] dotnet build 0 errors
- [ ] commit: "feat: F-23 report access control by role"
