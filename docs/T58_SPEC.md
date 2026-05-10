# T-58: F-16 HR实时通知

## 任务来源
PRD §3.5 F-16：HR实时通知

## 需求描述
当候选人进入AI视频面试房间时，系统立即推送飞书消息/邮件通知HR。

## 触发机制
- 触发点：`CandidateInterview.vue`中`verifyToken()`成功后，调用`aiInterviewSessionApi.start(sessionId)`将session状态改为`InProgress`
- 通知时机：session状态变为`InProgress`的那一刻

## 技术方案

### 后端改动

#### 1. 新增通知类型
**文件：** `Models/Entities/NotificationLog.cs`
```csharp
public enum NotificationType
{
    InterviewInvitation,
    InterviewReminder,
    Offer,
    InterviewStarted   // ← 新增
}
```

#### 2. 新增飞书通知服务
**文件：** `Services/FeishuNotificationService.cs`（新建）

```csharp
public interface IFeishuNotificationService
{
    Task<bool> SendInterviewStartedNotificationAsync(int sessionId);
}

public class FeishuNotificationService : IFeishuNotificationService
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public FeishuNotificationService(TalentPilotDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<bool> SendInterviewStartedNotificationAsync(int sessionId)
    {
        var session = await _dbContext.AIInterviewSessions
            .Include(s => s.Candidate)
            .Include(s => s.JobPost)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return false;

        var feishuWebhook = _configuration["Feishu:WebhookUrl"];
        if (string.IsNullOrEmpty(feishuWebhook)) return false;

        var message = new
        {
            msg_type = "interactive",
            card = new
            {
                header = new
                {
                    title = new { tag = "plain_text", content = $"🎤 候选人已进入面试 — {session.Candidate?.Name}" },
                    template = "red"
                },
                elements = new object[]
                {
                    new { tag = "div", content = new { tag = "plain_text", content = $"**职位：** {session.JobPost?.Title}" } },
                    new { tag = "div", content = new { tag = "plain_text", content = $"**面试时间：** {DateTime.UtcNow:yyyy-MM-dd HH:mm}" } },
                    new { tag = "hr" },
                    new { tag = "note", elements = new[] { new { tag = "plain_text", content = "候选人已准时进入AI面试房间，请HR准备实时监控或稍后查看面试报告。" } } }
                }
            }
        };

        using var client = new HttpClient();
        var response = await client.PostAsJsonAsync(feishuWebhook, message);
        return response.IsSuccessStatusCode;
    }
}
```

#### 3. 修改AIInterviewSessionService.StartAsync
**文件：** `Services/AIInterviewSessionService.cs`

在`StartAsync`方法中，成功将会话状态改为`InProgress`后，调用飞书通知：

```csharp
// 在 StartAsync 成功后调用（用 Fire-and-Forget，不阻塞主流程）
_ = Task.Run(async () =>
{
    try
    {
        var feishuSvc = sp.GetRequiredService<IFeishuNotificationService>();
        await feishuSvc.SendInterviewStartedNotificationAsync(session.Id);
    }
    catch { /* 不要因为通知失败影响面试流程 */ }
});
```

或者更简单的方案：在Controller的`StartSession` action中调用：

```csharp
// 在 AIInterviewSessionsController.StartSession 中，session成功启动后
var feishuSvc = HttpContext.RequestServices.GetRequiredService<IFeishuNotificationService>();
_ = feishuSvc.SendInterviewStartedNotificationAsync(id); // fire-and-forget
```

#### 4. 配置项
**文件：** `appsettings.json`（或`appsettings.Development.json`）
```json
{
  "Feishu": {
    "WebhookUrl": "YOUR_FEISHU_WEBHOOK_URL"
  }
}
```

### 前端改动
**无需前端改动**。触发点在后端`StartSession` action（候选人前端进入房间调用`POST /api/ai-interview-sessions/{id}/start`）。

## 验收条件
- [ ] `NotificationType.InterviewStarted` 枚举值存在
- [ ] `FeishuNotificationService.cs` 文件创建，包含飞书卡片消息发送
- [ ] `AIInterviewSessionService.StartAsync` 或 `StartSession` controller action 调用飞书通知
- [ ] `appsettings.Development.json` 包含 `Feishu.WebhookUrl` 配置项（可为空字符串）
- [ ] `dotnet build` 0 errors

## 文件清单
- `Services/FeishuNotificationService.cs`（新建）
- `Models/Entities/NotificationLog.cs`（修改：新增枚举值）
- `appsettings.Development.json`（修改：新增配置项）
- 可选：`Services/AIInterviewSessionService.cs` 或 `Controllers/AIInterviewSessionsController.cs`（视实现位置而定）
