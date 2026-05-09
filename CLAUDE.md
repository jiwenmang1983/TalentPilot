# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

TalentPilot 是一个 AI 驱动的智能招聘系统，自动化招聘全流程：**职位发布 → 跨渠道分发 → 简历采集 → AI 匹配筛选 → AI 实时面试 → 结构化面试报告**

核心业务模块：职位管理、简历采集解析、8维度AI智能匹配、AI视频面试、面试报告生成

## 技术栈

| 层级 | 技术 |
|---|---|
| 前端 | Vue 3 + Vite + Ant Design Vue 4 + Pinia + Vue Router + ECharts |
| 后端 | ASP.NET Core 8 + Entity Framework Core |
| 数据库 | MySQL 8.0 (Pomelo provider) |
| AI | MiniMax API (LLM for resume parsing, matching, interview questions, report generation) |

## 开发服务

```bash
# 启动后端 API (端口 5010)
cd backend/TalentPilot.Api && dotnet run

# 启动前端 (端口 5173)
cd frontend && npm run dev

# E2E 测试
cd frontend && npm run test:e2e
```

## 数据库约定

- Entity 新增字段后，**必须手动执行 SQL** 更新生产表（不依赖 EF migration 自动应用）
- 开发环境 MySQL: `mysql -h 127.0.0.1 -u root -pSandvik2026! talentpilot`

## MiniMax API 调用规范

- **Base URL**: `https://api.minimaxi.com/anthropic/v1/messages`
- **Headers**: `x-api-key: <key>` (NOT Bearer), `anthropic-version: 2023-06-01`
- **Model**: `MiniMax-M2.7`
- **调用方式**: 通过 CCS profile `ccs minimax-ai claude --print --max-turns 30`
- **禁止**手动 `claude --model` 指定模型

## API 响应格式

```csharp
public class ApiResponse<T> {
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
}
```

## JWT 认证

- Secret: `TalentPilotSecretKey2026VeryLongAndSecure` (configurable via `JwtSettings:SecretKey`)
- Issuer/Audience: `TalentPilot` / `TalentPilotUsers`
- Header: `Authorization: Bearer <token>`
- 自定义 JWT 中间件: `Middleware/JwtMiddleware.cs`

## API 文档

- Swagger UI: `http://localhost:5010/swagger`
- XML 注释已配置，所有 Controller 需要 `/// <summary>` 和 `[ProducesResponseType]` 注释

## 后端架构

```
backend/TalentPilot.Api/
  Controllers/          # API 端点 (14个 Controller)
  Services/             # 业务逻辑 (每个 Controller 对应一个 Service)
  Models/
    Entities/           # EF Entity (20+ 实体)
    DTOs/               # Request/Response DTO
  Data/
    TalentPilotDbContext.cs  # EF Core DbContext
    Migrations/         # EF migrations
  Middleware/
    JwtMiddleware.cs    # JWT 认证中间件
```

关键 Service：AIInterviewSessionService、MatchingService、MiniMaxService、InterviewReportService、JobPostService

## 前端架构

```
frontend/src/
  views/                # 页面组件 (按模块: analytics, auth, interview, layout, recruitment, system)
    layout/MainLayout.vue   # 主布局
  api/                  # Axios API 封装 (index.js + 各模块)
  stores/               # Pinia 状态管理 (auth store)
  router/index.js       # Vue Router 路由配置
  components/           # 公共组件
  utils/                # 工具函数
```

路由特点：候选人端面试流程 (`/interview/confirm/:token`, `/interview/candidate`) 无需登录

## 核心数据模型

| 实体 | 说明 |
|---|---|
| Users / Roles / RolePermissions | 用户权限体系 |
| Departments | 部门 (树形自关联) |
| Candidates | 候选人主记录 |
| JobPosts | 职位 |
| Resumes | 解析后简历 |
| MatchResults | 匹配记录 (8维度评分) |
| InterviewInvitations | 面试邀请 (含 InviteToken) |
| AIInterviewSessions | AI 面试会话 (含 SessionToken) |
| InterviewReports | 面试报告 |
| ConversionFunnels | 招聘漏斗转化数据 |

## PRD 驱动开发

- `docs/PRD.md` 是需求功能的**唯一事实来源**
- `docs/WBS.md` 是开发任务的追踪入口
- 任务状态: 🔄 待开发 → 🔄 开发中 → ✅ 已验收 → ✅ 已合并
- 当前阶段（探索期）：CC 直接写 main，不走 PR 流程

## Git 规范

- 分支: `main` (default)
- Commit 后立即 `git push`
- **禁止** `git force-push`（除非 Mark 明确审批）
- Commit 格式: `<type>: <简短描述>` (type: feat | fix | docs | refactor | test | chore)
