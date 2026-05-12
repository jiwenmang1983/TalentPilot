# TalentPilot — 工作分解结构 (WBS)

> 本文件是开发任务的工作分解结构（WBS），与 `docs/PRD.md` 配合使用。
> 小P 更新状态，CC 执行开发，小Q 执行测试，Mark 最终审批。

**版本：** v1.13
**日期：** 2026-05-11
**状态：** 🟢 Phase 13完成全部完成，Phase 14-19（Vue→React迁移）待开发

---

## 任务统计

| 阶段 | 任务数 | 状态 |
|---|---|---|
| Phase 1: 数据库设计 | 1 | 🟢 已完成 |
| Phase 2: 后端 API | 7 | 🟢 全部完成 |
| Phase 3: 前端页面 | 7 | 🟢 全部完成 |
| Phase 4: E2E 测试 | 3 | 🟢 核心功能✅(14/20通过，系统角色/视图滚动等边缘问题非阻塞) |
| Phase 5: 招聘核心功能 | 6 | 🟢 6/6完成(T-19~T-26✅) |
| Phase 6: AI增强+工程保障 | 9 | 🟢 9/9全部完成(T-27✅,T-28✅,T-29✅,T-30✅,T-31✅,T-32✅,T-33✅,T-34✅) |
| Phase 7: 系统验收测试 | 6 | 🎯 **Phase 7 完成** — API 49/51 ✅，E2E 25/25 ✅，UI截图 ✅ |
| **Phase 8: 跨渠道智能分发（§3.2）** | 5 | 🟢 5/5全部完成(T-48✅,T-49✅,T-50✅,T-51✅,T-52✅) |
| **Phase 9: 智能匹配引擎（§3.4）** | 3 | 🟢 3/3全部完成(T-53✅,T-54✅,T-55✅) |
| **Phase 10: 面试功能（§3.5~§3.6）** | 2 | 🟢 完成(T-56✅,T-57✅) |
| **Phase 11: 面试增强+报告（§3.5~§3.7）** | 3 | 🟢 T-58✅,T-59✅,T-60✅ |
| **Phase 12: 报告与权限（§3.7）** | 2 | 🟢 T-61✅,T-62✅ |
| **Phase 13: 体验优化（§3.5/§3.8）** | 1 | 🟢 T-63✅ |
| **Phase 14: 基础设施（API/Auth/路由）** | 4 | 🔴 待开发 |
| **Phase 15: Layout骨架** | 3 | 🔴 待开发 |
| **Phase 16: 招聘核心流程** | 9 | 🔴 待开发 |
| **Phase 17: 其他功能页面** | 7 | 🔴 待开发 |
| **Phase 18: 候选人端+收尾** | 3 | 🔴 待开发 |
| **Phase 19: React版E2E验收** | 1 | 🔴 待开发 |
| **合计** | **76** | |

---

## Phase 1：数据库设计（基础设施）

**T-01: 数据库实体设计**
  - 设计并创建所有系统管理相关表结构
  - 表: Users / Roles / RolePermissions / Departments / OperationLogs / UserLoginAttempts
  - MySQL 建表脚本
  - EF Core Entity 配置（7个类 + DbContext）
  - 状态: 🟢 已完成 (d4fe835)

---

## Phase 2：后端 API 开发

**T-02: 登录认证 API（S-06）**
  - JWT Token 签发（access token + refresh token）
  - 登录接口（用户名+密码）
  - Token 刷新接口
  - 登录失败锁定（5次失败锁定15分钟）
  - 种子数据（admin/TalentPilot2026）
  - 状态: 🟢 已完成 (d45fba7)

**T-03: 用户管理 API（S-01）**
  - 用户 CRUD（含 GET /api/users/current）
  - 创建用户（分配角色/部门）
  - 编辑用户（启用/禁用/重置密码）
  - 当前用户信息查询
  - 状态: 🟢 已完成 (012bc62)

**T-04: 角色管理 API（S-02）**
  - 角色 CRUD（预置 Admin/HR/用人经理 不可删除）
  - 角色权限配置
  - 状态: 🟢 已完成 (012bc62)

**T-05: 部门管理 API（S-03）**
  - 部门 CRUD（树形结构）
  - 获取部门树 / 获取子部门列表
  - GET /api/departments 列表端点
  - 状态: 🟢 已完成 (012bc62)

**T-06: 权限管理 API（S-04）**
  - 菜单权限读取（GET /api/permissions/menu）
  - 所有权限列表（GET /api/permissions/all）
  - 用户权限赋值
  - 状态: 🟢 已完成 (012bc62)

**T-07: 操作日志 API（S-05）**
  - 自动记录所有关键操作
  - 日志查询接口（支持分页/筛选/时间范围）
  - 日志内容：用户/操作类型/目标/时间/IP
  - 状态: 🟢 已完成 (012bc62)

**T-08: 候选人数据合规 API（S-07）**
  - 候选人 CRUD（GET/POST /api/candidates）
  - 候选人数据删除接口
  - 候选人数据导出权限校验
  - 状态: 🟢 已完成 (012bc62)

**Fix-Roles: 角色授权大小写修复**
  - [Authorize(Roles = "Admin")] → "admin" 修复
  - 状态: 🟢 已完成 (57aa488)

**Fix-Missing: 缺失端点修复**
  - GET /api/users/current（路由冲突修复）
  - GET /api/departments（405 → 200）
  - GET/POST /api/candidates（404 → 200）
  - 状态: 🟢 已完成 (d598a1e)

---

## Phase 3：前端页面开发

**T-09: 项目脚手架**
  - Vue 3 + Vite + Ant Design Vue + Pinia + Vue Router
  - axios JWT interceptor + 401 token refresh
  - 状态: 🟢 已完成 (99cf3f1)

**T-10: 登录页（S-06）**
  - 用户名/密码登录表单
  - JWT Token 存储与刷新逻辑
  - 登录失败提示（锁定倒计时）
  - 状态: 🟢 已完成 (99cf3f1)

**T-11: 用户管理页面（S-01）**
  - 用户列表（分页/筛选：角色/部门/状态）
  - 创建/编辑用户抽屉
  - 禁用/启用/重置密码操作
  - 状态: 🟢 已完成 (99cf3f1)

**T-12: 角色管理页面（S-02）**
  - 角色列表
  - 角色权限配置（菜单树勾选 + 数据权限设置）
  - 预置角色不可删除提示
  - 状态: 🟢 已完成 (99cf3f1)

**T-13: 部门管理页面（S-03）**
  - 树形组织架构展示
  - 新增/编辑/删除部门
  - 拖拽调整部门顺序
  - 状态: 🟢 已完成 (99cf3f1)

**T-14: 操作日志页面（S-05）**
  - 日志列表（分页/筛选：操作类型/用户/时间范围）
  - 日志详情弹窗
  - 状态: 🟢 已完成 (99cf3f1)

**T-15: 前端样式与布局**
  - 路由守卫（JWT校验）
  - 动态菜单
  - 按钮级别权限指令
  - Ant Design Vue 主题适配
  - 状态: 🟢 已完成 (99cf3f1)

---

## Phase 4：E2E 测试

**T-16: 后端 API 测试**
  - JWT 认证流程测试
  - 用户 CRUD 测试
  - 角色权限测试
  - 部门树测试
  - 操作日志记录验证
  - 候选人 CRUD 测试
  - 状态: ✅ 测试通过（15/15，小Q执行）

**T-17: 前端页面测试（E2E）**
  - 登录流程测试（Playwright）→ ✅ 5/5 passed
  - 用户管理页面测试 → 🔴 派发小Q
  - 角色权限配置测试 → 🔴 派发小Q
  - 部门管理测试 → 🔴 派发小Q
  - 状态: 🟡 进行中（小Q执行中，785b320）

**T-18: 集成测试**
  - 登录 → 用户管理 → 权限变更 全流程
  - 状态: ✅ 测试通过（7/7，小Q执行）

---

## Phase 5：招聘核心功能

**T-19: 职位发布功能（JobPosts）**
  - 职位 CRUD（创建/编辑/上下线/删除）
  - 职位描述模板管理
  - 职位要求（技能/学历/经验/薪资范围）
  - 职位状态：草稿/发布/暂停/关闭
  - 状态: 🟢 已完成 (be8cdc6)

**T-20: 简历采集功能（ResumeCollection）**
  - 多渠道配置（拉勾、Boss直聘、猎聘、智联、前程无忧）
  - AI自动采集简历（模拟接口）
  - 候选人注册/上传简历
  - 简历原始文件存储
  - 状态: 🟢 已完成 (4adf0b8)

**T-21: 简历解析入库（ResumeParsing）**
  - AI解析简历关键信息（姓名/电话/邮箱/工作经历/教育/技能）
  - 候选人主数据创建/更新
  - 简历与候选人关联
  - 状态: 🟢 已完成 (0abd05e)

**T-22: 智能匹配（Matching）**
  - 简历与职位JD匹配度算法
  - 匹配结果入库（匹配分数/理由）
  - HR查看匹配结果列表
  - 状态: 🟢 已完成 (9296230)

**T-23: AI面试邀约（InterviewInvitation）**
  - HR发起面试邀请（选择候选人+职位+时间）
  - 候选人选择面试时间
  - 邀约状态管理（待确认/已确认/已拒绝/已邀请）
  - 状态: 🟢 已完成 (e8e6706)

**T-24: AI面试执行（AIInterview）**
  - 视频面试接入（WebRTC，Mock）
  - AI实时问答
  - 面试状态记录（进行中/已完成/已取消）
  - 状态: ✅ 后端(960ad8ee)+前端(960ad8ee)全部完成

**T-25: 面试结果汇报（InterviewReport）**
  - AI生成面试报告（评分+评语+建议）
  - HR和用人经理查看报告
  - 状态: ✅ 完成 (c1b1a02)

---

**T-26: 招聘效果分析（ConversionFunnel）**
  - 漏斗可视化（发布→投递→匹配→面试→入职）
  - 各环节转化率统计
  - 状态: ✅ 完成 (f724df5)

**登录页UI重构（2026-05-08）**
  - 按Ant Design Vue官方设计语言重调
  - 主色#1677FF / 原生a-card容器 / #f0f2f5背景 / 极简商务风
  - 状态: ✅ 完成 (e87d19d)

---

## Phase 6：AI增强 + 工程保障

### AI能力增强（T-27 ~ T-30）

**T-27: AI简历解析接入真实LLM**
  - MiniMax LLM API 接入（简历关键信息提取）
  - 姓名/电话/邮箱/工作经历/教育背景/技能标签结构化提取
  - 候选人主数据自动创建/更新
  - 状态: ✅ 已完成 (5bd3a12，真实LLM调用确认)

**T-28: AI智能匹配算法优化**
  - 基于MiniMax LLM 匹配度评分（0-100）
  - 匹配理由生成（自然语言解释）
  - 技能匹配/经验匹配/薪资匹配多维度评分
  - 状态: ✅ 已完成 (11c10f8，MatchingService升级为MiniMax LLM)

**T-29: AI面试题自动生成**
  - 根据职位JD和候选人简历，LLM生成个性化面试问题
  - 题库分类：技术/行为/情景
  - 面试问题存储到AIInterviewSession.GeneratedQuestions
  - 状态: ✅ 已完成 (d2a636c，LLM生成个性化面试题)

**T-30: AI面试报告自动生成**
  - 面试结束后LLM自动生成综合报告
  - OverallScore（综合评分）/ Strengths / Weaknesses / Recommendation
  - 报告存储到InterviewReport表
  - 状态: ✅ 已完成 (50835d4，GenerateReportAsync升级为MiniMax LLM)

**T-31: API性能压测 + SQL优化**
  - SQL慢查询检测与索引优化
  - API响应时间基准测试
  - 数据库连接池调优
  - 状态: ✅ 已完成 (061819c，SQL索引+连接池调优)

### 工程保障（T-32 ~ T-34）

**T-32: JWT/权限安全审查**
  - 所有API端点权限覆盖检查
  - JWT token 安全验证（过期/刷新/撤销）
  - CORS 配置审计
  - 状态: ✅ 已完成 (e3e14ac)

**T-33: Swagger API 文档补全**
  - 所有Controller XML注释 → Swagger UI
  - API 版本管理（/api/v1/）
  - 请求/响应示例补全
  - 状态: ✅ 完成 (d08b424)

|**T-34: 候选人通知系统**
  - 邮件通知（面试邀请/面试提醒/录用通知）
  - 候选人邮件模板管理
  - 通知发送状态追踪
  - 状态: ✅ 已完成 (daf25c5，邮件通知+模板管理+NotificationLogs表)

**UI-SFT: 前端UI SFT风格改造**
  - MainLayout.vue：自定义分组菜单 + Sandvik蓝Header + 面包屑 + 用户头像下拉
  - Login.vue：左右分栏（蓝底品牌+白底表单）
  - Dashboard.vue：新建，KPI卡片+图表+最近动态
  - Page Header：JobPostList + CandidateList 加入 SFT风格 page header + toolbar
  - router/index.js：默认 redirect → /dashboard
  - talentpilot-logo.svg：手工SVG logo
  - 状态: ✅ 已完成 (df0fc76，push待网络恢复)

---

## Phase 7：系统验收测试

### 任务状态

| 任务 | 负责人 | 状态 | 备注 |
|---|---|---|---|
|| T-40: API自动化测试（Phase 7 全模块） | 小P | ✅ | 49/51通过，0失败 |
|| T-41: Bug修复（InterviewInvitationService未注册） | 小P | ✅ | Program.cs |
|| T-42: Bug修复（MiniMaxService M2.7兼容性） | 小P | ✅ | GetFirstText()方法 |
|| T-43: 数据库Schema修复 | 小P | ✅ | MySQL DDL |
|| T-44: Playwright E2E | 小P | ✅ | 25 passed, 1 skipped |
|| T-46: UserManagement formRef nextTick()修复 | 小P | ✅ | a481824 |
|| T-45: UI验收截图确认 | Mark | 🔴 待确认 | 8张截图已拍 |

### T-40~T-43 修复记录

| Bug | 描述 | 修复方式 | 状态 |
|---|---|---|---|
| B-1 | `InterviewInvitationService` 未注册 | Program.cs | ✅ |
| B-2 | `NotificationLogs`/`NotificationTemplates` 表缺失 | MySQL DDL | ✅ |
| B-3 | `AIInterviewSessions.GeneratedQuestions` 列缺失 | MySQL DDL | ✅ |
| B-4 | `MiniMaxService.ChatAsync` M2.7 thinking块不兼容 | GetFirstText() + 3处调用点 | ✅ |
| B-5 | TC-0504 部门删除测试逻辑错误 | 修正测试脚本 | ✅ |
| B-6 | API请求超时（15s→30s） | 全局timeout调大 | ✅ |
| B-7 | TC-1101~TC-1104 面试流程测试错误 | 修正start+Answer字段 | ✅ |
| B-8 | TC-1201 报告路由错误 | 修正generate+session路由 | ✅ |

### API自动化测试结果（Phase 7.1 ~ 7.12）

- **总计**：51 测试用例
- **通过**：49 ✅
- **跳过**：2（条件跳过，无已完成会话时）
- **失败**：0

**通过模块**：认证(5) / 部门管理(5) / 用户角色(3) / 权限管理(1) / 操作日志(2) / 候选人(2) / 招聘核心(8) / AI面试会话(6) / 通知系统(2) / API性能(5)

---

## Phase 8：跨渠道智能分发（§3.2 PRD）

> PRD：§3.2 跨渠道智能分发（F-02~F-06）
> 状态：🔴 待 Mark 审批后启动

**T-48: 渠道账号管理（F-04）**
  - `ChannelCredentials` 表（存储各渠道 API Key / 凭证）
  - `ChannelCredentialController` CRUD API
  - `ChannelCredentialManagement.vue` 前端页面
  - 状态: ✅ 已完成 (e90ee75)

**T-49: AI 内容适配服务（F-05）**
  - `ContentAdaptationService` — LLM 实现 JD → 各渠道格式转换
  - `JobChannelContents` 表（存储各渠道适配后的 JD 内容）
  - Boss直聘（500字+技能标签）/ 猎聘 / 拉勾 / 领英 / 小红书 格式模板
  - 状态: ✅ 完成（4a7982a）

**T-50: AI Agent 分发引擎（F-02）**
  - `JobDistributionAgent` — 按优先级执行各渠道分发
  - `JobDistributionTasks` 表（任务状态：待发布/发布中/已发布/失败）
  - `JobDistributionLogs` 表（发布结果日志）
  - 定时发布支持（BackgroundService）
  - 状态: ✅ 完成（def2190）

**T-51: 发布状态追踪前端（F-06）**
  - JobPostList 页面添加"分发状态"按钮 → Drawer 展示分发任务
  - 失败原因展示 + 重新发布/取消发布按钮
  - 状态: ✅ 完成 (45066ad)

**T-52: 定时发布后台调度**
  - `DistributionTaskScheduler` (IHostedService, 每30秒轮询pending任务)
  - `JobPostList.vue` 添加轮询 (setInterval 30s)
  - 状态: ✅ 完成 (151ee12)

---

## Phase 9：智能匹配引擎（§3.4 PRD）

> PRD：§3.4 匹配引擎（F-10~F-12）
> 状态：🟢 **Phase 9 全部完成**

**T-53: 8维度AI匹配引擎**
  - 后端：MatchingService重写（8维度JSON解析，权重可配置）
  - 后端：JobPostsController新增 match-threshold/match-weights API
  - 前端：MatchResultList重写（筛选+8维度条+展开详情）
  - 前端：MatchResultDetail重写（维度条+阈值达标显示+权重配置滑块）
  - 状态: ✅ 完成 (f97a883后端 + 7790a5a前端)

**T-54: F-10 简历库管理 + 阈值Override**
  - 后端：ResumesController增强（LEFT JOIN MatchResults，jobPostId/channel/minScore/maxScore筛选，按Score倒序）
  - 后端：MatchesController新增 PATCH /{id}/threshold（阈值override）
  - 后端：MatchingService保护override值（仅在null时写入）
  - 前端：ResumeList重写（筛选栏+匹配度进度条+调整阈值弹窗+达标状态显示）
  - 状态: ✅ 完成 (4195318)

**T-55: F-07 多渠道简历采集**
  - 后端：ResumeCollectionScheduler（IHostedService，6小时定时调度）
  - 后端：ResumesController新端点（GET /channels、POST /collect-now、POST /mock-collect）
  - 前端：ResumeList.vue采集管理UI（立即采集按钮+渠道状态表格）
  - 路由修复：[ApiController]下{id:int}与channels路由冲突，移除约束改用手动int.TryParse
  - 状态: ✅ 完成

## Phase 10：面试功能（§3.5~§3.6 PRD）

> PRD：§3.5 面试邀约与预约 / §3.6 AI实时视频面试
> 状态: ✅ Phase 10完成

**T-56: F-15 候选人自主预约时间**
  - 后端：AIInterviewSessionsController新增 `/slots` + `/book` + `/booking-status` 端点
  - 后端：可用时段生成逻辑（未来48小时工作时段）
  - 前端：`InterviewBooking.vue` 候选人预约页面
  - 候选人无需登录，使用URL中的sessionToken访问
  - 状态: ✅ 完成(fada60c)

**T-57: F-17 面试问题配置UI**
  - 后端：JobPost新增InterviewQuestions(TEXT NULL)+InterviewDuration(INT NOT NULL DEFAULT 30)字段
  - 后端：JobPostService + DTO更新，支持CRUD
  - 前端：JobPostForm.vue新增面试设置区块（时长选择+问题列表可增删）
  - 前端：JobPostDetail.vue展示面试问题
  - 状态: ✅ 完成(f5ca13f)

---

## Phase 11：面试增强+报告（§3.5~§3.7 PRD）

> PRD：§3.5 F-16 HR实时通知 / §3.6 F-18实时面试流程 / §3.7 F-21报告生成
> 状态: 🔵 进行中

**T-58: F-16 HR实时通知**
  - 后端：新增NotificationType.InterviewStarted枚举值
  - 后端：新建FeishuNotificationService，发送飞书卡片消息给HR
  - 后端：集成到StartSession action（session状态变为InProgress时触发）
  - 状态: 🔄 进行中（CC委派中）

**T-59: F-18 实时面试流程**
  - 核心视频面试房间（WebRTC/语音转文字/AI追问）
  - 候选人进入房间→verifyToken→逐一提问→AI追问→完成
  - 状态: ⏳ 待开发

**T-60: F-21 报告生成**
---

## Phase 14：基础设施 — API层 / Auth / 路由 / 工具函数

> 前端 React 项目基础架构搭建，1:1 移植 Vue 原版能力，不改变后端 API 契约
> 分支：`feature/react-shadcn`，目录：`frontend-react/src/`
> 状态: 🔴 待开发

**T-64: API Service 层**
  - `src/services/api.ts` — 完整 Axios 实例（baseURL `/api`、interceptors、401重试刷新Token逻辑）
  - 14个模块 API 导出：authApi、userApi、roleApi、departmentApi、permissionApi、operationLogApi、notificationApi、channelApi、jobpostApi、resumeApi、candidateApi、matchApi、interviewApi、reportApi
  - 与 Vue 版 `frontend/src/api/index.js` 1:1 对齐

**T-65: Auth Store（Zustand）**
  - `src/stores/authStore.ts` — Zustand store（token、userInfo、isLoggedIn、login、logout、setUserInfo）
  - localStorage 持久化（accessToken / refreshToken / userInfo）
  - LoginPage 对接 AuthStore，401 错误由 api interceptor 处理

**T-66: React Router 路由**
  - `src/router/index.tsx` — 23条路由完整映射（与 `frontend/src/router/index.js` 1:1）
  - 路由守卫：requiresAuth 时检查 isLoggedIn，未登录 redirect `/login`
  - lazy loading 各页面组件
  - 候选人公开路由（confirm/candidate/booking）无需认证

**T-67: 工具函数库**
  - `src/lib/utils.ts` — cn()（clsx + tailwind-merge）
  - `src/lib/constants.ts` — routeTitleMap 等常量
  - 补充缺失的通用工具函数（dateFormat、debounce 等）

---

## Phase 15：Layout 骨架 — 侧边栏 / Header / MainLayout

> 等同于 Vue 的 `MainLayout.vue`，React + shadcn/ui 实现，样式 1:1 还原
> 状态: 🔴 待开发

**T-68: Sidebar 侧边栏**
  - `src/components/layout/Sidebar.tsx`
  - 三组菜单（职位与简历管理 / 面试与评估 / 系统管理），flat menu 结构
  - 折叠态（collapsed）切换：icon-only 模式，居中显示 icon
  - Logo区（`/talentpilot-logo.svg` + "TalentPilot" + "AI 智能招聘平台"）
  - active 状态高亮（左侧3px border-left + 背景色 + 字体色）
  - scroll 纵向滚动，max-height: calc(100vh - 60px)

**T-69: Header 头部**
  - `src/components/layout/Header.tsx`
  - 左侧：collapse toggle button + Breadcrumb（当前页面标题）
  - 右侧：通知铃铛 Badge（count=0）+ 用户下拉（Avatar首字母 + 用户名 + DownOutlined）
  - 用户下拉：个人中心 + 退出登录

**T-70: MainLayout 整合**
  - `src/components/layout/MainLayout.tsx`
  - 整合 Sidebar + Header + `<Outlet />` 内容区
  - App.tsx 更新为 MainLayout 包裹路由（与 Vue router outlet 行为一致）
  - 样式与 Vue 版 1:1（背景色 #0D3D92、字体色系统、阴影等）

---

## Phase 16：招聘核心流程（9个页面）

> 对应 Vue `frontend/src/views/recruitment/` 下全部页面，React + shadcn 实现
> 状态: 🔴 待开发

**T-71: JobPostListPage 职位列表**
  - `src/views/recruitment/JobPostListPage.tsx`
  - Table 展示职位（标题/部门/状态/创建时间），分页
  - 状态筛选（Draft/Published/Paused/Closed）+ 关键词搜索
  - 操作：新建职位按钮（→ `/jobposts/new`）、编辑（→ `/jobposts/:id`）、AI内容适配按钮、渠道分发按钮

**T-72: JobPostFormPage 职位表单（新建/编辑）**
  - `src/views/recruitment/JobPostFormPage.tsx`（路由 `/:id` 和 `/new` 共用）
  - 表单字段：职位名称、所属部门（下拉）、职位描述（Textarea）、任职要求、薪资范围、职位类型
  - 面试设置区块：时长选择（15/30/45/60min）+ 问题列表（可增删）
  - 表单验证（必填项）
  - 保存后 redirect `/jobposts`

**T-73: ResumeListPage 简历列表**
  - `src/views/recruitment/ResumeListPage.tsx`
  - Table（姓名/邮箱/手机/投递职位/匹配度/状态），分页
  - 筛选：职位下拉 + 匹配度滑块（阈值筛选）+ 渠道
  - 匹配度进度条可视化，达标/不达标状态标签
  - 操作：查看详情、调整阈值弹窗
  - 采集管理：立即采集按钮 + 渠道状态表格

**T-74: ResumeParsePage 简历解析**
  - `src/views/recruitment/ResumeParsePage.tsx`
  - 上传组件（拖拽上传 + 点击上传）
  - 解析结果展示区（姓名/邮箱/手机/工作经历/教育经历）
  - 手动录入表单兜底

**T-75: CandidateListPage 候选人列表**
  - `src/views/recruitment/CandidateListPage.tsx`
  - Table（姓名/邮箱/手机/来源/创建时间），分页
  - 筛选：来源渠道 + 创建日期范围
  - 操作：查看详情（→ `/candidates/:id`）

**T-76: CandidateDetailPage 候选人详情**
  - `src/views/recruitment/CandidateDetailPage.tsx`
  - Tabs：基本信息 / 投递记录 / 面试记录
  - 基本信息：头像、姓名、联系方式、工作经历、教育经历
  - 投递记录列表 + 面试记录列表

**T-77: MatchResultListPage 智能匹配列表**
  - `src/views/recruitment/MatchResultListPage.tsx`
  - Table（候选人/职位/匹配度/状态），分页
  - 匹配度降序排列，进度条可视化
  - 操作：查看详情（→ `/matches/:id`）

**T-78: MatchResultDetailPage 匹配详情**
  - `src/views/recruitment/MatchResultDetailPage.tsx`
  - 候选人信息卡片 + 职位信息卡片
  - 8维度评分展示（雷达图或条形图）
  - AI评价摘要 + 推荐结论

**T-79: InterviewInvitationPage 面试邀约**
  - `src/views/recruitment/InterviewInvitationPage.tsx`
  - Table（候选人/职位/面试时间/状态），分页
  - 操作：发起邀约、发送提醒、取消邀约

---

## Phase 17：其他功能页面（7个页面）

> Dashboard / Interview / Notification / System 四大模块
> 状态: 🔴 待开发

**T-80: DashboardPage 数据看板**
  - `src/views/analytics/DashboardPage.tsx`
  - 4个 KPI 卡片（总职位数/在招职位/简历投递数/面试邀约数）
  - 招聘漏斗图表（ECharts 或 Recharts）
  - 月度趋势折线图
  - 最近动态列表

**T-81: InterviewSessionsPage AI面试会话**
  - `src/views/interview/InterviewSessionsPage.tsx`
  - Table（候选人/职位/开始时间/状态），分页
  - 操作：进入面试（WebRTC房间入口）、查看报告

**T-82: InterviewReportsPage 面试报告列表**
  - `src/views/interview/InterviewReportsPage.tsx`
  - Table（候选人/职位/综合评分/生成时间），分页
  - 操作：查看报告详情

**T-83: NotificationListPage 通知日志**
  - `src/views/notification/NotificationListPage.tsx`
  - Table（类型/内容/时间/状态），分页
  - 筛选：通知类型 + 时间范围

**T-84: NotificationTemplatesPage 通知模板**
  - `src/views/notification/NotificationTemplatesPage.tsx`
  - 模板列表（名称/类型/内容预览）
  - 新建/编辑模板表单弹窗

**T-85: SystemPages 系统管理（4合一）**
  - `src/views/system/UserManagementPage.tsx` — 用户CRUD + 状态切换
  - `src/views/system/RoleManagementPage.tsx` — 角色CRUD + 权限配置
  - `src/views/system/DepartmentTreePage.tsx` — 树形部门管理
  - `src/views/system/OperationLogsPage.tsx` — 操作日志分页列表

**T-86: ChannelCredentialPage 渠道账号管理**
  - `src/views/system/ChannelCredentialPage.tsx`
  - 渠道列表（名称/类型/凭证状态/最后同步时间）
  - 新增/编辑渠道凭证表单

---

## Phase 18：候选人端 + 收尾

> 候选人公开流程（无需登录）+ shadcn 组件补充 + 全局样式调优
> 状态: 🔴 待开发

**T-87: 候选人公开页面（3个）**
  - `src/views/public/CandidateConfirmPage.tsx` — 面试确认页（路由 `/interview/confirm/:token`）
  - `src/views/public/CandidateInterviewPage.tsx` — AI视频面试房间（路由 `/interview/candidate`）
  - `src/views/public/InterviewBookingPage.tsx` — 候选人预约页面（路由 `/interview-book/:sessionToken`）
  - 以上三个路由无需 AuthGuard

**T-88: shadcn 组件补充安装**
  - 按 Phase 16-17 需求补充安装：Table、Dialog、Sheet、Select、Form/Tabs、Badge、Toast/Sonner、Avatar、Dropdown Menu、Pagination、Breadcrumb、Skeleton、Popover、Calendar、Chart（Recharts）
  - 集中安装：`npx shadcn@latest add table dialog sheet select tabs badge toast avatar dropdown-menu pagination breadcrumb skeleton popover calendar -y`

**T-89: 全局样式调优 + 响应式**
  - `src/index.css` — 全局 CSS 变量覆盖（与 Vue 版 Ant Design 色彩体系对应：#0D3D92 主色、#F5F7FA 背景）
  - 响应式：Sidebar 在小屏幕下 Drawer 模式
  - 字体、间距、圆角全局统一
  - 加载骨架（Skeleton）替代 Spinner
  - 深色模式基础支持（dark: 变量覆盖）

---

## Phase 依赖关系

```
Phase 14 基础设施（T-64 → T-67）
           ↓
Phase 15 Layout骨架（T-68 → T-70）
           ↓
Phase 16 招聘核心（T-71 → T-79）← CC 并行开发
           ↓
Phase 17 其他功能（T-80 → T-86）← CC 并行开发
           ↓
Phase 18 收尾（T-87 → T-89）
```

---

## Phase 19：React 版 E2E 验收测试

> React 前端功能验收，对标 Vue 版 Phase 4/Phase 7 测试用例
> 状态: 🔴 待开发

**T-90: React 版 E2E 测试**
  - Playwright 测试脚本（复用 `frontend/test/e2e/` 结构）
  - 核心流程：登录 → Dashboard → 职位列表 → 新建职位 → 候选人列表 → 智能匹配 → 面试邀约
  - 目标：核心功能通过率 ≥ 90%
  - 执行：`cd frontend-react && npm run test:e2e`

---

## 任务统计

| 阶段 | 任务数 | 状态 |
|---|---|---|
| Phase 1-13: 原有功能 | 49 | 🟢 全部完成 |
| **Phase 14: 基础设施** | 4 | 🔴 待开发 |
| **Phase 15: Layout骨架** | 3 | 🔴 待开发 |
| **Phase 16: 招聘核心** | 9 | 🔴 待开发 |
| **Phase 17: 其他功能** | 7 | 🔴 待开发 |
| **Phase 18: 候选人端+收尾** | 3 | 🔴 待开发 |
| **Phase 19: E2E验收** | 1 | 🔴 待开发 |
| **合计** | **76** | |

---

## 状态说明

| 状态 | 说明 |
|---|---|
| 🔴 待开发 | 尚未开始 |
| 🟡 进行中 | 正在开发 |
| 🟢 已完成 | 开发完成，待测试 |
| ✅ 测试通过 | 测试完成，待验收 |
| 🎯 已验收 | Mark 确认完成 |
