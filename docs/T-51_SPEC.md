# T-51 SPEC: 发布状态追踪前端（Drawer 方案）

## 任务目标
在 JobPostList 页面添加"分发状态"按钮，点击后弹出 Drawer 展示该职位在各渠道的分发状态，支持重新发布操作。

## 功能范围

### 1. 分发状态按钮
- 在 JobPostList 每行操作列添加"分发状态"按钮（蓝色）
- 点击后打开 Drawer，侧边展示该职位的所有分发任务

### 2. Drawer 内容
- 标题：职位名称 + "的分发状态"
- 内嵌表格显示分发任务（来自 `GET /api/distribution/tasks/job/{jobPostId}`）
- 列：渠道名称 | 任务状态 | 计划发布时间 | 实际执行时间 | 失败原因 | 操作

### 3. 状态展示
- pending（等待中）- 灰色
- running（执行中）- 蓝色
- succeed（已成功）- 绿色
- failed（失败）- 红色，显示 failureReason
- cancelled（已取消）- 灰色

### 4. 操作按钮
- 重新发布：触发 retry（`PUT /api/distribution/tasks/{id}/retry`），重置状态为 pending
- 取消发布：触发 cancel（`PUT /api/distribution/tasks/{id}/cancel`）
- 刷新：重新加载任务列表

### 5. 实时刷新
- Drawer 打开时自动加载最新任务状态
- 支持手动刷新按钮

## 技术方案

### 后端（已实现 ✅）
- `GET /api/distribution/tasks/job/{jobPostId}` - 获取职位所有分发任务
- `PUT /api/distribution/tasks/{id}/retry` - 重试任务
- `PUT /api/distribution/tasks/{id}/cancel` - 取消任务

### 前端（JobPostList.vue 改造）
1. 在操作列添加"分发状态"按钮
2. 新增 Drawer 组件（侧边抽屉，600px 宽）
3. Drawer 内嵌 `<a-table>` 显示分发任务
4. 新增 ref: `distStatusDrawerVisible` + `distStatusJobPost` + `distStatusTasks`
5. 新增函数: `openDistStatusDrawer(jobPost)`, `closeDistStatusDrawer()`, `loadDistTasks()`, `retryDistTask(id)`, `cancelDistTask(id)`

### API 文件（已存在）
- `frontend/src/api/jobDistribution.js` — 已有 `getByJob(jobPostId)` / `retryTask(id)` / `cancelTask(id)`

## UI 布局

### Drawer 布局
```
┌──────────────────────────────────┐
│ 职位名称 的分发状态          [X] │
├──────────────────────────────────┤
│ [刷新]                           │
├──────────────────────────────────┤
│ 渠道 │ 状态 │ 计划 │ 执行 │ 原因 │
│ ─────┼──────┼──────┼──────┼─────│
│ 猎聘 │  ●   │ 10:00 │ 10:01 │  -  │
│ Boss │  ✗   │ 10:00 │  -    │ 超时│
└──────────────────────────────────┘
```

## 验收标准
1. ✅ 点击"分发状态"按钮，打开 Drawer 显示该职位分发任务
2. ✅ 各状态用不同颜色标签展示
3. ✅ 失败任务显示具体失败原因
4. ✅ 重新发布按钮触发重试，任务状态变为 pending
5. ✅ 取消发布按钮触发取消
6. ✅ 刷新按钮更新任务列表
