# T-56 SPEC: F-15 候选人自主预约

## 1. 需求概述

**PRD：** §3.5.1 F-15 候选人自主预约时间

候选人点击面试邀请链接后，可自主选择面试时间（未来48小时内可用时段），确认后系统通知HR。

---

## 2. 功能拆解

### 2.1 后端 API

**新增 Endpoint：**

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/interview-slots/{sessionId}` | 获取指定会话的可用时段 |
| POST | `/api/interview-slots/{sessionId}/book` | 候选人预约时间 |
| GET | `/api/interview-sessions/{id}/booking-status` | 获取预约状态 |

**数据库变更：**
- `AIInterviewSessions` 表新增字段：
  - `BookingDeadline` (datetime, nullable) — 最晚预约时间
  - `InterviewDuration` (int, minutes, default=30)
  - `AvailableSlots` (nvarchar, JSON数组) — 可用时段，如 `["2026-05-12T09:00","2026-05-12T10:00","2026-05-12T14:00"]`

**Slots 生成逻辑：**
- 生成未来48小时内的工作时间时段（9:00-12:00, 14:00-18:00），每小时间隔
- 排除已预约的时段

### 2.2 前端页面

**新页面：** `/interview-book/:sessionToken` （候选人无需登录）

内容：
1. 面试介绍（职位名称 + 公司 + 面试时长）
2. 日历/时段选择器（显示未来48小时可用时段）
3. 已选时段确认
4. 提交后显示"预约成功，等待HR通知"

---

## 3. 技术约束

- 候选人访问无需JWT token，使用一次性 `sessionToken`（URL参数）
- sessionToken 验证通过后才显示预约页面
- 预约成功后发邮件/飞书通知HR（复用通知系统）

---

## 4. 验收条件

- [ ] GET `/api/interview-slots/{id}` 返回可用时段数组
- [ ] POST `/api/interview-slots/{id}/book` 返回200，状态更新
- [ ] 候选人前端页面可正常显示和预约
- [ ] build 0 errors
