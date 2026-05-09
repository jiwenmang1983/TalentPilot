# PRD Logic Gaps — TalentPilot §3.2 跨渠道智能分发

> 本文件记录 PRD 与实际实现的逻辑差距。Q&A 结论直接融入 PRD 各章节，不只留在本文件。
> 更新：2026-05-10

---

## §3.2 跨渠道智能分发 — 未实现

### F-02 职位发布（AI Agent 自动分发）

**PRD 描述：**
- HR 确认 JD 后点击"发布"，AI Agent 自动执行分发流程
- 发布前可预览各渠道适配后的内容
- 支持"定时发布"（设置发布时间窗口）
- 发布状态实时追踪：待发布 → 发布中 → 已发布 / 发布失败
- 发布失败时显示失败原因

**实际实现：**
- JobPosts 表有 `status` 字段（草稿/发布中/暂停/已结束）
- 有 JobPostsController CRUD
- **无 AI Agent 分发逻辑**
- **无跨渠道发布状态追踪**
- **无定时发布机制**

**缺口：**
- `JobDistributionTasks` 表（不存在）— 记录每个渠道的分发任务和状态
- `ChannelDistributionAgent` 服务（不存在）— AI Agent 执行分发逻辑
- `JobDistributionLogs` 表（不存在）— 每个渠道的发布结果

---

### F-04 渠道账号管理

**PRD 描述：**
- HR 在后台配置各渠道的接入凭证
- 支持渠道：猎聘/拉勾/Boss直聘/领英/小红书/自定义链接

**实际实现：**
- **无渠道账号管理 UI**
- **无渠道凭证 API**

**缺口：**
- `ChannelCredentials` 表（不存在）— 存储各渠道 API Key / 账号密码
- `ChannelCredentialController`（不存在）
- `ChannelCredentialManagement.vue`（不存在）

---

### F-05 AI 内容适配

**PRD 描述：**
- AI Agent 将 JD 内容自动适配为各渠道要求的格式
- Boss直聘：500字以内，自动提取关键技能标签
- 猎聘：按猎聘 JD 模板格式填充
- 拉勾：适配拉勾的职位亮点字段
- 领英：适配领英的职位描述格式
- 小红书：转化为社交化招聘文案

**实际实现：**
- **无 AI 内容适配逻辑**
- **无渠道内容模板**

**缺口：**
- `JobChannelContents` 表（不存在）— 存储各渠道适配后的 JD 内容
- `ContentAdaptationService`（不存在）— AI LLM 调用实现格式转换
- 各渠道内容模板配置（数据库或配置文件）

---

### F-06 发布状态追踪

**PRD 描述：**
- 状态：待发布 / 发布中 / 已发布 / 部分失败 / 发布失败

**实际实现：**
- JobPosts 表 `status` 字段只有：Draft / Publishing / Paused / Closed
- **无渠道级发布状态**

**缺口：**
- `JobDistributionTasks` 表需有：`jobId`, `channel`, `status`, `publishedUrl`, `errorMessage`, `publishedAt`
- 前端 JobPostList 需显示各渠道发布状态

---

## §3.3 AI 简历采集 — 已实现

| 功能 | 状态 | 说明 |
|---|---|---|
| F-07 多渠道简历采集 | ✅ 已实现 | T-20，ResumeCollection |
| F-08 简历解析 | ✅ 已实现 | T-21，ResumeParsing |
| F-09 跨渠道去重 | ✅ 已实现 | Candidates 表按姓名+手机号去重 |
| F-10 简历库管理 | ✅ 已实现 | CandidatesController + CandidateList.vue |

---

## §3.4 AI 简历匹配 — 已实现

| 功能 | 状态 | 说明 |
|---|---|---|
| F-11 匹配引擎（8维度） | ✅ 已实现 | T-22，MatchingService，MiniMax LLM |
| F-12 阈值规则与分流 | ✅ 已实现 | 阈值80，>80待通知，60-80备选池，<60不合适 |
| F-13 匹配透明度 | 🟡 部分实现 | 各维度得分有，扣分原因说明不完整 |

---

## §3.5 候选人通知与面试邀请 — 已实现

| 功能 | 状态 | 说明 |
|---|---|---|
| F-14 面试邀请发送 | ✅ 已实现 | T-23，InterviewInvitation，邮件+短信 |
| F-15 候选人自主预约时间 | ✅ 已实现 | timeSlotStart/End 字段 |
| F-16 HR 实时通知 | ✅ 已实现 | NotificationService，邮件通知 |

---

## §3.6 AI 实时视频面试 — 已实现

| 功能 | 状态 | 说明 |
|---|---|---|
| F-17 面试问题配置 | ✅ 已实现 | T-23，HR预设问题+AI动态追问 |
| AI 追问机制 | ✅ 已实现 | T-29，AIInterviewSession.GeneratedQuestions |
| 面试报告生成 | ✅ 已实现 | T-25/T-30，InterviewReport + MiniMax LLM |

---

## §3.7 招聘漏斗 — 已实现

| 功能 | 状态 | 说明 |
|---|---|---|
| ConversionFunnel 漏斗 | ✅ 已实现 | T-26，ConversionFunnelsController + 可视化 |

---

## 结论

**必须实现（PRD §3.2）：**
1. F-02 AI Agent 分发（核心）
2. F-04 渠道账号管理（基础设施）
3. F-05 AI 内容适配（核心）
4. F-06 发布状态追踪（配套）

**建议 Mark 决策：**
- 是立即开发 §3.2（Phase 8）
- 还是当前 Phase 1-7 状态即可接受
