# T-57 SPEC: F-17 面试问题配置UI

## 1. 需求概述

**PRD：** §3.6 F-17 面试问题配置

HR在创建/编辑职位时，配置面试预设问题（3-5题）+面试时长设置。

---

## 2. 功能拆解

### 2.1 后端 API

**修改 JobPostsController/JobPostService：**

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/job-posts/{id}` | 返回jobPost含interviewQuestions |
| PUT | `/api/job-posts/{id}` | 更新时含interviewQuestions |
| POST | `/api/job-posts` | 创建时含interviewQuestions |

**JobPosts表新增字段：**
- `InterviewQuestions` (TEXT/JSON) — JSON数组，如 `["问题1","问题2","问题3"]`
- `InterviewDuration` (INT, 分钟，默认30)

### 2.2 前端

**修改 JobPostForm.vue / JobPostDetail.vue：**

- 新增"面试设置"区块
- 预设问题列表（3-5题，可增删改）
- 面试时长选择器（15/20/30/45/60分钟）

---

## 3. 验收条件

- [ ] GET `/api/job-posts/{id}` 返回 interviewQuestions 和 interviewDuration
- [ ] PUT 更新职位时保留面试问题
- [ ] 前端JobPostForm显示问题配置区块
- [ ] build 0 errors
