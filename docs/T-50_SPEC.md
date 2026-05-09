# T-50 SPEC: AI Agent 分发引擎（F-02）

> Phase 8 §3.2 跨渠道智能分发 — T-50
> 依赖：T-48（渠道凭证）+ T-49（AI内容适配）

## 1. 概述

`JobDistributionAgent` 按优先级执行职位分发（定时+立即），使用 T-48 渠道凭证 + T-49 适配内容，追踪发布状态。

**接入策略：**
1. 官方 Open API（最优先）
2. 第三方聚合平台（无官方API时）
3. 浏览器自动化（无API时兜底）

---

## 2. MySQL 表设计

### JobDistributionTasks 表
```sql
CREATE TABLE JobDistributionTasks (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    JobPostId BIGINT NOT NULL,
    ChannelType VARCHAR(20) NOT NULL,      -- liepin/lagou/boss/linkedin/xiaohongshu/custom
    TaskStatus VARCHAR(20) NOT NULL DEFAULT 'pending',  -- pending/running/success/failed
    ScheduledAt DATETIME NULL,             -- 定时发布时间（PublishTime）
    StartedAt DATETIME NULL,
    CompletedAt DATETIME NULL,
    FailureReason VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_job_channel (JobPostId, ChannelType),
    INDEX idx_status (TaskStatus),
    INDEX idx_scheduled (ScheduledAt)
);
```

### JobDistributionLogs 表
```sql
CREATE TABLE JobDistributionLogs (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    TaskId BIGINT NOT NULL,
    LogLevel VARCHAR(10) NOT NULL,         -- info/warn/error
    Message VARCHAR(1000) NOT NULL,
    Details TEXT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_task (TaskId)
);
```

### JobPosts 表扩展
已有列：`TargetChannels`, `PublishTime`（T-48 创建 JobChannelContents 时已添加）

---

## 3. 后端 Entity

### JobDistributionTask.cs
```csharp
// 位置：Models/Entities/JobDistributionTask.cs
public class JobDistributionTask
{
    public long Id { get; set; }
    public long JobPostId { get; set; }
    public string ChannelType { get; set; }  // liepin/lagou/boss/linkedin/xiaohongshu/custom
    public string TaskStatus { get; set; }   // pending/running/success/failed
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public JobPost? JobPost { get; set; }
    public List<JobDistributionLog> Logs { get; set; }
}
```

### JobDistributionLog.cs
```csharp
// 位置：Models/Entities/JobDistributionLog.cs
public class JobDistributionLog
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public string LogLevel { get; set; }   // info/warn/error
    public string Message { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public JobDistributionTask? Task { get; set; }
}
```

---

## 4. DTOs

### JobDistributionTaskDtos.cs
```csharp
// 位置：Models/Entities/JobDistributionTaskDtos.cs

public record DistributionTaskDto(
    long Id,
    long JobPostId,
    string JobTitle,
    string ChannelType,
    string ChannelName,
    string TaskStatus,
    string StatusText,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? FailureReason,
    DateTime CreatedAt
);

public record CreateDistributionTaskDto(
    long JobPostId,
    List<string> ChannelTypes,
    DateTime? ScheduledAt  // null = 立即发布
);

public record TriggerDistributionDto(
    long JobPostId,
    List<string> ChannelTypes
);

public record DistributionLogDto(
    long Id,
    long TaskId,
    string LogLevel,
    string Message,
    string? Details,
    DateTime CreatedAt
);

public record DistributionResultDto(
    long TaskId,
    string ChannelType,
    string TaskStatus,
    string? FailureReason,
    List<DistributionLogDto> Logs
);
```

---

## 5. Services

### JobDistributionService.cs
```csharp
// 位置：Services/JobDistributionService.cs
// 核心职责：
// 1. 创建分发任务（支持立即+定时）
// 2. 执行分发（调用渠道API/自动化）
// 3. 追踪状态
// 4. 查询任务列表和日志
```

**核心方法：**

1. `CreateTasks(JobPostId, ChannelTypes, ScheduledAt)` — 创建任务（状态=pending）
2. `ExecuteTask(TaskId)` — 执行单个分发任务
3. `ExecuteAllPending()` — 执行所有 pending + scheduled<=now 的任务
4. `GetTasksByJob(JobPostId)` — 获取职位的所有分发任务
5. `GetTaskLogs(TaskId)` — 获取任务日志
6. `GetTaskById(TaskId)` — 获取单个任务详情
7. `CancelTask(TaskId)` — 取消待执行任务（仅 pending 可取消）

**分发执行流程（ExecuteTask）：**
1. 读取 `JobChannelContents`（T-49 的适配内容）
2. 读取 `ChannelCredentials`（T-48 的凭证）
3. 根据渠道类型选择分发方式：
   - `liepin` → HTTP POST 到猎聘 Open API
   - `lagou` → HTTP POST 到拉勾 API
   - `boss` → 日志记录（浏览器自动化待 T-50 后期）
   - `linkedin` → 日志记录（浏览器自动化待 T-50 后期）
   - `xiaohongshu` → 日志记录（浏览器自动化待 T-50 后期）
   - `custom` → 生成文本内容供 HR 手动发布
4. 更新 `TaskStatus`（success/failed）
5. 记录 `JobDistributionLogs`

**ExecuteTask 模拟实现（初期）：**
由于各平台 API 需要企业认证账号，初期实现为：
- 更新 TaskStatus = running（StartedAt = now）
- 模拟等待 2-5 秒（模拟 API 调用）
- 更新 TaskStatus = success（CompletedAt = now）
- 添加 info 日志："职位内容已推送至 {ChannelName}"

这样可以完成完整的状态流转，后期替换为真实 API 调用即可。

### JobDistributionBackgroundService.cs
```csharp
// 位置：Services/JobDistributionBackgroundService.cs
// 继承 BackgroundService
// 每 60 秒轮询一次：ExecuteAllPending()
// 启动时执行一次
```

---

## 6. Controller

### JobDistributionController.cs
```csharp
// 位置：Controllers/JobDistributionController.cs
// Route: api/distribution

[HttpPost("trigger")]  // 触发分发
public async Task<IActionResult> TriggerDistribution([FromBody] TriggerDistributionDto dto)

[HttpPost("tasks")]    // 创建任务（定时+立即）
public async Task<IActionResult> CreateTasks([FromBody] CreateDistributionTaskDto dto)

[HttpGet("tasks/{id}")]  // 获取任务详情
public async Task<IActionResult> GetTask(long id)

[HttpGet("tasks/job/{jobPostId}")]  // 获取职位的所有任务
public async Task<IActionResult> GetTasksByJob(long jobPostId)

[HttpDelete("tasks/{id}")]  // 取消任务
public async Task<IActionResult> CancelTask(long id)

[HttpGet("tasks/{id}/logs")]  // 获取任务日志
public async Task<IActionResult> GetTaskLogs(long id)

[HttpGet("logs/job/{jobPostId}")]  // 获取职位的所有日志
public async Task<IActionResult> GetJobDistributionLogs(long jobPostId)
```

---

## 7. DbContext 配置

### TalentPilotDbContext.cs 添加：
```csharp
public DbSet<JobDistributionTask> JobDistributionTasks { get; set; }
public DbSet<JobDistributionLog> JobDistributionLogs { get; set; }

// OnModelCreating:
modelBuilder.Entity<JobDistributionTask>(entity =>
{
    entity.HasIndex(e => new { e.JobPostId, e.ChannelType });
    entity.HasIndex(e => e.TaskStatus);
    entity.HasIndex(e => e.ScheduledAt);
});

modelBuilder.Entity<JobDistributionLog>(entity =>
{
    entity.HasIndex(e => e.TaskId);
    entity.Property(e => e.Message).HasMaxLength(1000);
    entity.Property(e => e.Details).HasColumnType("text");
});
```

### Program.cs 添加：
```csharp
builder.Services.AddScoped<JobDistributionService>();
builder.Services.AddHostedService<JobDistributionBackgroundService>();
```

---

## 8. 前端（JobPostList.vue 改造）

### jobDistribution.js API
```javascript
// 位置：frontend/src/api/jobDistribution.js
export const jobDistributionApi = {
  trigger(jobPostId, channelTypes) {
    return axios.post('/distribution/trigger', { jobPostId, channelTypes })
  },
  createTasks(jobPostId, channelTypes, scheduledAt) {
    return axios.post('/distribution/tasks', { jobPostId, channelTypes, scheduledAt })
  },
  getTask(id) {
    return axios.get(`/distribution/tasks/${id}`)
  },
  getTasksByJob(jobPostId) {
    return axios.get(`/distribution/tasks/job/${jobPostId}`)
  },
  cancelTask(id) {
    return axios.delete(`/distribution/tasks/${id}`)
  },
  getTaskLogs(taskId) {
    return axios.get(`/distribution/tasks/${taskId}/logs`)
  }
}
```

### JobPostList.vue 添加：

**新增列：**
在表格中新增"发布状态"列（展开行 expansion），显示各渠道的分发任务状态。

**新增工具栏按钮：**
- "立即发布" — 调用 `trigger` API
- "定时发布" — 弹出日期时间选择器 → 调用 `createTasks` API

**展开行内容：**
```html
<a-table expanded row...>
  <template #expandedRowRender="{ record }">
    <!-- 分发状态列表 -->
    <div v-for="task in getDistributionTasks(record.id)" :key="task.id">
      <span>{{ task.channelName }}</span>
      <a-tag :color="getStatusColor(task.taskStatus)">{{ task.statusText }}</a-tag>
      <span v-if="task.failureReason" style="color:red">{{ task.failureReason }}</span>
    </div>
  </template>
</a-table>
```

**实现说明：**
1. 展开行渲染时调用 `getTasksByJob(jobId)` 获取分发任务
2. "立即发布"按钮：调用 `trigger` API → 刷新任务列表
3. "定时发布"按钮：弹出 a-modal（日期时间选择器）→ 调用 `createTasks` API
4. 任务状态颜色：`pending`=blue, `running`=orange, `success`=green, `failed`=red

---

## 9. 验收标准

1. ✅ `dotnet build` 编译通过
2. ✅ API 端点全部可访问（无 404）
3. ✅ `CreateTasks` → pending 状态任务创建成功
4. ✅ `TriggerDistribution` → 任务从 pending → running → success
5. ✅ `GetTasksByJob` → 返回任务列表含状态
6. ✅ `CancelTask` → pending 任务可取消
7. ✅ `GetTaskLogs` → 返回日志列表
8. ✅ BackgroundService 每 60s 扫描一次
9. ✅ 前端"立即发布"按钮可触发分发流程
10. ✅ 前端展开行显示各渠道发布状态

---

## 10. 位置索引

| 文件 | 路径 |
|------|------|
| DDL | `docs/T-50_DDL.sql` |
| SPEC | `docs/T-50_SPEC.md`（本文件） |
| Entity | `backend/.../Models/Entities/JobDistributionTask.cs` |
| Entity | `backend/.../Models/Entities/JobDistributionLog.cs` |
| DTOs | `backend/.../Models/Entities/JobDistributionTaskDtos.cs` |
| Service | `backend/.../Services/JobDistributionService.cs` |
| BackgroundService | `backend/.../Services/JobDistributionBackgroundService.cs` |
| Controller | `backend/.../Controllers/JobDistributionController.cs` |
| DbContext | `backend/.../Data/TalentPilotDbContext.cs`（patch） |
| Program.cs | `backend/.../Program.cs`（patch） |
| 前端API | `frontend/src/api/jobDistribution.js` |
| 前端UI | `frontend/src/views/recruitment/JobPostList.vue`（patch） |
| 前端API index | `frontend/src/api/index.js`（patch） |
