# T-49 SPEC: AI内容适配服务（F-05）

> 内容适配：将职位JD转化为各渠道标准格式
> 负责人：CC（小P验收）

---

## 1. 概述

**目标：** LLM 将职位描述（JD）自动适配为各渠道标准格式，存储到 `JobChannelContents` 表，供后续分发使用。

**核心流程：**
```
职位 JD（原始文本）
    ↓ ContentAdaptationService
各渠道适配内容（猎聘/拉勾/Boss/领英/小红书/自定义）
    ↓ 存储 JobChannelContents 表
JobDistributionAgent 读取并分发
```

---

## 2. 数据模型

### JobChannelContents 表（MySQL）

```sql
CREATE TABLE JobChannelContents (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    JobPostId INT NOT NULL,
    ChannelType VARCHAR(20) NOT NULL,          -- liepin/lagou/boss/linkedin/xiaohongshu/custom
    ChannelName VARCHAR(50) NOT NULL,
    AdaptedTitle VARCHAR(200),                  -- 渠道专属标题
    AdaptedContent TEXT,                          -- 适配后正文（HTML/Markdown）
    WordCount INT DEFAULT 0,
    SkillTags JSON,                             -- 技能标签数组
    SalaryMin INT,
    SalaryMax INT,
    Status VARCHAR(20) DEFAULT 'pending',       -- pending/ready/failed
    AdaptationPrompt TEXT,                       -- 使用的Prompt快照
    ErrorMessage VARCHAR(500),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_job_channel (JobPostId, ChannelType),
    INDEX idx_status (Status)
);
```

---

## 3. 适配格式规范

### 3.1 Boss直聘（boss）
- **标题格式：** `[城市]职位名称 | 薪资范围 | 学历要求`
- **内容结构：**
  - 第一段：职位亮点（3条，用`·`分隔）
  - 第二段：职位描述（200字，简练）
  - 第三段：技能要求（5~8个标签）
  - 第四段：公司介绍（50字）
- **总字数：** 500~800字
- **技能标签：** 显示为蓝色标签

### 3.2 猎聘（liepin）
- **标题格式：** `职位名称（城市·薪资）`
- **内容结构：**
  - 职位概要（100字）
  - 岗位职责（3~5条，每条30字）
  - 任职要求（3~5条，每条30字）
  - 技能标签（JSON数组）
- **总字数：** 600~1000字

### 3.3 拉勾（lagou）
- **标题格式：** `职位名称_公司名_城市`
- **内容结构：**
  - 职位诱惑（3条）
  - 职位描述（3~5条职责）
  - 任职要求（3~5条要求）
  - 技能要求（标签形式）
- **总字数：** 400~700字

### 3.4 领英（linkedin）
- **标题格式：** `职位名称 at 公司名`
- **内容结构：**
  - About the role（80字）
  - Responsibilities（3~5条bullet）
  - Requirements（3~5条bullet）
  - Skills（标签列表）
- **总字数：** 300~600字（英文为准）

### 3.5 小红书（xiaohongshu）
- **标题格式：** `[招聘]职位名称 | 城市 | 薪资`
- **内容结构：**
  - 开场白（emoji + 20字吸引句）
  - 职位亮点（3条，每条带emoji）
  - 职位描述（简洁）
  - 技能要求（标签）
  - 投递方式
- **总字数：** 200~400字（口语化，轻松风格）

### 3.6 自定义（custom）
- 允许用户自定义模板
- 变量占位符：`{{title}}` `{{salary}}` `{{requirements}}` 等

---

## 4. API 设计

### ContentAdaptationController (`/api/job-channel-contents`)

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/job-channel-contents?jobPostId={id}` | 获取职位所有渠道内容 |
| GET | `/api/job-channel-contents/{channelType}?jobPostId={id}` | 按渠道获取 |
| POST | `/api/job-channel-contents/adapt` | 触发适配（body: {jobPostId, channelTypes[]}) |
| PUT | `/api/job-channel-contents/{id}` | 手动编辑适配内容 |
| DELETE | `/api/job-channel-contents/{id}` | 删除适配内容 |

### Request/Response DTOs

```csharp
// POST /api/job-channel-contents/adapt
public class AdaptContentRequest {
    public int JobPostId { get; set; }
    public List<string> ChannelTypes { get; set; }  // ["boss","liepin","lagou"]
}

// GET /api/job-channel-contents?jobPostId=1
public class JobChannelContentDto {
    public int Id { get; set; }
    public int JobPostId { get; set; }
    public string ChannelType { get; set; }
    public string ChannelName { get; set; }
    public string AdaptedTitle { get; set; }
    public string AdaptedContent { get; set; }
    public int WordCount { get; set; }
    public List<string> SkillTags { get; set; }
    public string Status { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

## 5. ContentAdaptationService 实现

### 5.1 核心方法

```csharp
public class ContentAdaptationService {
    // 主适配方法
    public async Task<Dictionary<string, JobChannelContent>> AdaptJobPostAsync(
        JobPost jobPost,
        List<string> channelTypes,
        string createdBy);

    // 单渠道适配
    private async Task<JobChannelContent> AdaptToChannelAsync(
        JobPost jobPost,
        string channelType,
        string createdBy);

    // 获取各渠道 Prompt 模板
    private string GetChannelPrompt(string channelType, JobPost jobPost);
}
```

### 5.2 Prompt 模板示例（猎聘）

```
你是一个专业的招聘文案专家。请将以下职位描述改编为猎聘招聘平台的格式。

原始职位信息：
- 职位名称：{{Title}}
- 工作城市：{{City}}
- 薪资范围：{{SalaryMin}}K-{{SalaryMax}}K
- 职位描述：{{Description}}
- 技能要求：{{Requirements}}

要求：
1. 标题格式：`职位名称（城市·薪资）`
2. 职位概要：100字，概括核心职责
3. 岗位职责：3~5条，每条30字左右
4. 任职要求：3~5条，每条30字左右
5. 技能标签：5~8个，从原技能要求中提取
6. 总字数：600~1000字

请直接输出改编后的内容，格式如下：
## 职位概要
[100字概要]

## 岗位职责
1. [第一条]
2. [第二条]
3. [第三条]

## 任职要求
1. [第一条]
2. [第二条]
3. [第三条]

## 技能标签
[标签1] [标签2] [标签3] ...
```

---

## 6. 前端集成

### JobPostList.vue 改造
- 在职位列表页面添加"适配内容"按钮
- 点击后弹出适配面板（选择渠道）
- 显示各渠道适配状态（pending/ready/failed）
- 快速预览/编辑适配内容

### 页面路径
- 新页面：`/job-channel-contents`（可选，也可以内嵌到 JobPostList）
- 复用现有 JobPostList 布局，增加渠道适配 tab

---

## 7. 技术约束

- LLM 调用使用现有的 `MiniMaxService`（复用 T-27~T-30 的基础设施）
- 每个渠道适配独立调用 LLM，失败不影响其他渠道
- 适配内容支持手动编辑（前端 PUT /api/job-channel-contents/{id}）
- 适配是幂等的：重复调用更新已有记录

---

## 8. 验收标准

- [ ] `JobChannelContents` 表创建成功
- [ ] `ContentAdaptationService` 注册到 Program.cs
- [ ] 6个渠道格式全部支持（boss/liepin/lagou/linkedin/xiaohongshu/custom）
- [ ] POST /api/job-channel-contents/adapt 触发适配成功
- [ ] GET /api/job-channel-contents?jobPostId=X 返回各渠道适配内容
- [ ] LLM 调用使用 MiniMax API（已有基础设施）
- [ ] 技能标签正确提取并以 JSON 数组存储
- [ ] 前端 JobPostList 显示适配状态入口
- [ ] 单元测试：各渠道 Prompt 生成正确

---

## 9. 文件清单

### 后端新增
```
backend/TalentPilot.Api/
  Models/Entities/
    JobChannelContent.cs          # Entity
    JobChannelContentDtos.cs     # DTOs
  Services/
    ContentAdaptationService.cs  # 核心适配逻辑
  Controllers/
    JobChannelContentsController.cs  # API 端点
  Data/
    TalentPilotDbContext.cs      # 添加 DbSet（需patch）
  Program.cs                     # 注册服务（需patch）
```

### MySQL DDL
```sql
CREATE TABLE JobChannelContents (...);
```

### 前端（可内嵌 JobPostList.vue 改造）
- 如新建页面：`views/recruitment/JobChannelContentManagement.vue`
- 如内嵌：在 JobPostList.vue 添加渠道适配 tab

---

## 10. 依赖关系

- **前置依赖：** T-48（渠道凭证管理）— 需要读取各渠道凭证类型
- **后置依赖：** T-50（JobDistributionAgent）— 读取 JobChannelContents 进行分发
