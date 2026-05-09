# TalentPilot — AI Recruitment Platform

## 项目概述
TalentPilot 是一个 AI 驱动的招聘管理平台，支持简历解析、智能匹配、AI 模拟面试和面试邀请管理。

## 技术栈
- **Backend**: ASP.NET Core 8.0 / C# / Entity Framework Core / MySQL (Pomelo)
- **Frontend**: Vue 3 + Vite + Ant Design Vue
- **AI**: MiniMax API (LLM for resume parsing, matching, interview questions, report generation)
- **API Port**: 5010
- **Frontend Port**: 5173
- **Database**: MySQL talentpilot (root/Sandvik2026!@127.0.0.1:3306)

## 关键约定

### Git
- 分支策略: `main` (default)
- Commit: 提交后立即 `git push`
- **禁止** `git force-push`（除非 Mark 明确审批）

### 数据库
- 新建 Entity 后，**必须**用 `mysql` 客户端手动建表（不依赖 EF migration）
- MySQL: `mysql -h 127.0.0.1 -u root -pSandvik2026! talentpilot`

### API 调用 (MiniMax)
- Base URL: `https://api.minimaxi.com/anthropic/v1/messages`
- Header: `x-api-key: <key>` (NOT Bearer)
- Header: `anthropic-version: 2023-06-01`
- Model: `MiniMax-M2.7`
- 调用方式: 通过 CCS profile `ccs minimax-ai claude --print --max-turns 30`
- **禁止**手动 `claude --model` 指定模型

### 目录结构
```
backend/TalentPilot.Api/
  Controllers/       # API 端点
  Services/          # 业务逻辑
  Models/
    Entities/         # EF Entity
    DTOs/             # Request/Response DTO
  Data/              # DbContext
  Middleware/         # JWT 中间件等
```

### API 响应格式
```csharp
public class ApiResponse<T> {
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
}
```

### JWT 认证
- JWT secret: `TalentPilotSecretKey2026VeryLongAndSecure`
- Issuer/Audience: `TalentPilot` / `TalentPilotUsers`
- Header: `Authorization: Bearer <token>`

### Swagger / API Docs
- Swagger UI: `http://localhost:5010/swagger`
- XML 注释已配置 (`GenerateDocumentationFile=true`)
- 所有 Controller 需要 `/// <summary>` 和 `[ProducesResponseType]` 注释

## 开发服务
```bash
# 启动 API
cd backend/TalentPilot.Api && dotnet run

# 启动前端
cd frontend && npm run dev

# 数据库 (MySQL)
mysql -h 127.0.0.1 -u root -pSandvik2026! talentpilot
```

## CC 任务规范
- 所有编码任务**必须先写任务规格文件** `/tmp/cc_task_T<NN>.txt`
- 五段式: 目标、工作目录、任务步骤、验收标准、结果文件
- 完成后写入 `/tmp/cc_result_T<NN>.json`
- dotnet build 成功后才能 git commit

## 数据库表 (主要)
- `Users` / `Roles` / `Permissions` — 权限体系
- `Departments` — 部门
- `Candidates` — 候选人（含 Email, Phone）
- `JobPosts` — 职位
- `Resumes` — 简历
- `Matches` — 匹配记录
- `InterviewInvitations` — 面试邀请（含 InviteToken）
- `AIInterviewSessions` — AI 面试会话
- `InterviewReports` — 面试报告
- `OperationLogs` — 操作日志
- `ConversionFunnels` — 转化漏斗
