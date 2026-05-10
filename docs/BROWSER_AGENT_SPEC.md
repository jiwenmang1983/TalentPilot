# Browser Agent 技术规格

**版本：** v0.1  
**日期：** 2026-05-10  
**状态：** 🟡 设计中

---

## 1. 目标

通过 AI Browser Agent 从 Boss 直聘等平台**截图采集**简历数据，绕过官方 API 资质门槛。

---

## 2. 核心架构

```
Windows 侧：
  Chrome --remote-debugging-port=9222
         ↓ WebSocket (CDP)
WSL 侧：
  Playwright C# Agent
    ↓
  1. 连接 CDP → 控制浏览器
  2. 导航到简历列表/详情页
  3. Screenshot → 保存到本地
  4. 发给 MiniMax Vision API
  5. 解析结构化数据 → 存入 DB
```

---

## 3. 组件设计

### 3.1 CDP Bridge（Windows 侧）

**启动方式（Mark 在 Windows CMD/PowerShell 执行）：**
```cmd
chrome.exe --remote-debugging-port=9222 --user-data-dir="C:\Users\<USER>\chrome-debug"
```

**触发时机：**
- Mark 首次手动启动一次 → 手动登录 Boss 直聘（解决滑块验证码）
- 后续 CDP Bridge 复用已保存的 Chrome 实例

### 3.2 Playwright Agent（WSL 侧）

**项目路径：** `backend/TalentPilot.Api/Services/BrowserAgent/`

**文件结构：**
```
BrowserAgent/
  BrowserAgentService.cs      # 主服务（BackgroundService）
  BossPlatform.cs             # Boss 直聘平台适配器
  PlaywrightBrowserManager.cs  # 浏览器生命周期管理
  VisionParser.cs             # MiniMax Vision 截图解析
  CookieSessionManager.cs     # Cookie/Session 持久化
  BrowserAgentController.cs   # API 控制接口
```

### 3.3 Cookie 持久化（绕过登录验证码）

**流程：**
1. Mark 首次手动在 Chrome（debug 模式）登录 Boss 直聘
2. Agent 首次运行时检测到无 cookie，**提示 Mark 手动登录一次**
3. Agent 从 CDP 导出 cookies，保存到 `browser_sessions/boss_cookies.json`
4. 后续启动时自动加载 cookies，复用登录态

**Cookie 刷新策略：**
- 每次启动检查 cookies 有效性（访问一个需登录的页面验证）
- 失效则 API 返回 401 → 提示 Mark 重新手动登录

### 3.4 Boss 直聘采集流程

```
1. GET https://www.zhipin.com/webchat/  (简历列表页)
2. 等待页面加载完成
3. 滚动加载更多（懒加载）
4. Screenshot 列表页
5. MiniMax Vision 解析：识别每个候选人卡片（姓名/职位/年龄/学历）
6. 点击进入详情页
7. Screenshot 详情页
8. MiniMax Vision 解析：工作经历/项目经历/自我介绍
9. 保存到 DB → 触发 AI 匹配
10. 返回列表页 → 重复直到无新数据
```

### 3.5 MiniMax Vision 截图解析

**API：**
- 端点：`POST https://api.minimaxi.com/anthropic/v1/messages`
- Model：`MiniMax-4k-Vision`（支持图片理解）
- Prompt：针对简历截图设计，提取结构化字段

**Prompt 示例（详情页）：**
```
你是一个专业的简历解析AI。请从截图中提取以下结构化信息，以JSON格式返回：
{
  "姓名": "",
  "性别": "",
  "年龄": "",
  "学历": "",
  "工作年限": "",
  "当前公司": "",
  "当前职位": "",
  "期望职位": "",
  "期望薪资": "",
  "工作经历": [{"公司": "", "职位": "", "时间": "", "描述": ""}],
  "项目经历": [{"项目名": "", "角色": "", "描述": ""}],
  "技能标签": [],
  "自我介绍": ""
}
如果某字段无法从截图中获取，请返回null。
只返回JSON，不要其他文字。
```

---

## 4. API 接口

### 4.1 触发采集

```
POST /api/browser-agent/collect
{
  "platform": "boss",        // boss | lagou | liepin
  "action": "resume_list",   // resume_list | resume_detail
  "url": "可选，自定义URL"
}
```

### 4.2 查询采集任务状态

```
GET /api/browser-agent/status/{taskId}
```

### 4.3 刷新登录态

```
POST /api/browser-agent/refresh-login
{
  "platform": "boss"
}
```

### 4.4 保存 Cookie（手动登录后）

```
POST /api/browser-agent/save-cookies
{
  "platform": "boss"
}
```

---

## 5. 数据模型

### BrowserAgentTask

| 字段 | 类型 | 说明 |
|---|---|---|
| Id | int | 主键 |
| Platform | string | 平台名称 |
| Action | string | 采集动作 |
| Status | enum | Pending / Running / Completed / Failed |
| TotalCount | int | 采集到的简历数 |
| StartedAt | DateTime | 开始时间 |
| CompletedAt | DateTime? | 完成时间 |
| ErrorMessage | string? | 错误信息 |

---

## 6. 技术依赖

| 组件 | 技术选型 | 版本 |
|---|---|---|
| 浏览器控制 | Playwright for .NET | v1.40+ |
| Chrome CDP | Windows Chrome | 最新版 |
| 截图解析 | MiniMax Vision API | MiniMax-4k-Vision |
| 会话存储 | JSON 文件 | `browser_sessions/*.json` |
| 定时调度 | .NET BackgroundService | 内置 |

---

## 7. 待 Mark 上机验证项

- [ ] Windows 侧启动 Chrome Debug Mode
- [ ] 手动登录 Boss 直聘一次
- [ ] 导出 cookies 给 Agent 使用
- [ ] 端到端 CDP Bridge 连通性测试
- [ ] 截图 + Vision API 解析准确率测试

---

## 8. 技术风险

| 风险 | 缓解方案 |
|---|---|
| Boss 直聘反爬检测 | 随机延时（1-3s）+ 随机滚动速度 |
| 滑块验证码 | Cookie 复用规避首次验证码 |
| 页面结构变化 | Vision AI 兜底，不依赖 DOM 解析 |
| MiniMax Vision 解析错误 | 增加 Prompt + 人肉抽检 |
| Chrome Debug 端口被占用 | 检查端口占用，提示 Mark 关闭其他 Chrome 实例 |
