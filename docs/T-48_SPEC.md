# T-48 实现规范：渠道账号管理（F-04）

> 本文件是 CC 开发执行的交付规范，等效于 CC 的"需求文档"。
> PRD 原文见 `docs/PRD.md` §3.2 F-04。
> 执行前必读：PLAYBOOK.md

---

## 1. 目标

HR 在后台配置各渠道的接入凭证，支持 6 个渠道：猎聘 / 拉勾 / Boss直聘 / 领英 / 小红书 / 自定义链接。

接入方式：
- 猎聘、拉勾：官方 Open API（API Key 认证）
- Boss直聘、领英、小红书：浏览器自动化（Cookie/Token）
- 自定义链接：纯文本 URL

---

## 2. 数据库设计

### 2.1 `ChannelCredentials` 表（新建）

```sql
CREATE TABLE ChannelCredentials (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ChannelType VARCHAR(20) NOT NULL UNIQUE,  -- liepin/lagou/boss/linkedin/xiaohongshu/custom
    ChannelName VARCHAR(50) NOT NULL,          -- 展示名：猎聘/拉勾/Boss直聘/领英/小红书/自定义
    AccessType VARCHAR(20) NOT NULL,           -- api_key / browser_auto / custom_url
    Credentials TEXT,                          -- JSON: {apiKey, apiSecret, cookie, token, etc.}
    CustomUrl VARCHAR(500),                    -- 仅 custom 类型使用
    IsEnabled TINYINT(1) NOT NULL DEFAULT 1,
    CreatedBy VARCHAR(100),
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME,
    IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
    INDEX idx_channel_type (ChannelType),
    INDEX idx_is_enabled (IsEnabled)
);
```

**Credentials 字段格式（JSON）：**
```json
// 猎聘/拉勾（API Key方式）
{"apiKey": "xxx", "apiSecret": "xxx"}

// Boss直聘/领英/小红书（浏览器自动化）
{"cookie": "xxx", "csrfToken": "xxx"}

// 自定义链接
{"url": "https://..."}
```

### 2.2 `JobPosts` 表扩展（ALTER）

```sql
ALTER TABLE JobPosts
  ADD COLUMN TargetChannels VARCHAR(200),   -- JSON数组: ["liepin","lagou","boss","linkedin","xiaohongshu","custom"]
  ADD COLUMN PublishTime DATETIME,          -- 定时发布时刻（可空表示立即）
  ADD COLUMN PublishedAt DATETIME;          -- 实际发布时间
```

---

## 3. 后端实现

### 3.1 Entity（新建）

`Models/Entities/ChannelCredential.cs`

```csharp
public class ChannelCredential
{
    public int Id { get; set; }
    public string ChannelType { get; set; }   // liepin | lagou | boss | linkedin | xiaohongshu | custom
    public string ChannelName { get; set; }
    public string AccessType { get; set; }    // api_key | browser_auto | custom_url
    public string? Credentials { get; set; }  // JSON string
    public string? CustomUrl { get; set; }
    public bool IsEnabled { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

### 3.2 DbContext（修改）

在 `TalentPilotDbContext.cs` 添加：
```csharp
public DbSet<ChannelCredential> ChannelCredentials { get; set; }
```

在 `OnModelCreating` 添加配置。

### 3.3 ChannelCredentialService（新建）

`Services/ChannelCredentialService.cs`

```csharp
public class ChannelCredentialService
{
    // GET /api/channel-credentials - 列表（脱敏返回）
    // GET /api/channel-credentials/{channelType} - 按渠道类型查
    // POST /api/channel-credentials - 创建（凭证加密存储）
    // PUT /api/channel-credentials/{channelType} - 更新
    // DELETE /api/channel-credentials/{channelType} - 软删除

    // GetCredentials(channelType) - 内部使用，返回解密后的JSON对象
    // ValidateCredentials(channelType) - 验证凭证是否有效（测试连接）
}
```

### 3.4 ChannelCredentialController（新建）

`Controllers/ChannelCredentialsController.cs`

```
GET    /api/channel-credentials              - 列表（脱敏，不返回实际key）
GET    /api/channel-credentials/{channelType} - 单个
POST   /api/channel-credentials              - 创建
PUT    /api/channel-credentials/{channelType} - 更新
DELETE /api/channel-credentials/{channelType} - 删除
POST   /api/channel-credentials/{channelType}/validate - 测试连接
```

### 3.5 JobPostsController 扩展（修改）

新增端点：
```
GET    /api/jobposts/{id}/distribution-status - 各渠道发布状态
POST   /api/jobposts/{id}/distribute          - 手动触发一次全渠道分发
```

---

## 4. 前端实现

### 4.1 API（新建）

`frontend/src/api/channel.js`

```js
export const channelApi = {
  list: () => api.get('/channel-credentials'),
  getByType: (type) => api.get(`/channel-credentials/${type}`),
  create: (data) => api.post('/channel-credentials', data),
  update: (type, data) => api.put(`/channel-credentials/${type}`, data),
  remove: (type) => api.delete(`/channel-credentials/${type}`),
  validate: (type) => api.post(`/channel-credentials/${type}/validate`)
}
```

### 4.2 页面（新建）

`frontend/src/views/system/ChannelCredentialManagement.vue`

**页面元素：**
- 页面标题：渠道账号管理
- 6 个渠道卡片，每个显示：渠道名称 / 接入方式 / 启用状态 / 操作按钮
- 点击"配置"打开 Drawer 表单
- Drawer 表单字段根据渠道类型动态变化：
  - 猎聘/拉勾：API Key + API Secret
  - Boss/领英/小红书：Cookie + CSRF Token
  - 自定义：URL 文本框
- "测试连接"按钮（调用 validate API）
- 凭证保存时加密传输

**Draw Form 字段：**
| 渠道 | 字段 |
|---|---|
| 猎聘/拉勾 | API Key, API Secret |
| Boss直聘 | Cookie, CSRF Token |
| 领英 | Cookie, Access Token |
| 小红书 | Cookie |
| 自定义 | URL 文本 |

---

## 5. 安全要求

- `ChannelCredential.Credentials` 字段保存时 AES 加密（密钥从 `appsettings.json` 读取）
- GET 接口返回时隐藏实际凭证值，只显示 `***` + 渠道类型
- 软删除（`IsDeleted = true`），不物理删除记录

---

## 6. 验收标准

1. ✅ 6 个渠道的凭证可以新增/编辑/删除
2. ✅ 凭证明文不在前端日志/响应中暴露
3. ✅ 测试连接功能正常（猎聘/拉勾可真实测通）
4. ✅ JobPosts 表增加了 `TargetChannels`、`PublishTime`、`PublishedAt` 字段
5. ✅ `GET /api/jobposts/{id}/distribution-status` 返回各渠道状态
6. ✅ `POST /api/jobposts/{id}/distribute` 触发分发（先做 mock，逐步接真实 API）
