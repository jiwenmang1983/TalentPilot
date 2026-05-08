# 小Q & CC 状态追踪

> Hermes 主动管理，任务不丢、上下文不断

---

## CC 状态

| 项目 | 值 |
| --- | --- |
| tmux session | cc-talentpilot |
| 状态 | 🟡 T-03 进行中（Users/Departments/Roles API） |
| 当前分支 | main |
| 当前任务 | T-03 用户管理 API（用户CRUD/部门树/角色权限） |
| 最后活动时间 | 2026-05-08 09:35 |

**CC 任务队列**

| # | 任务 | 状态 | 开始时间 |
| --- | --- | --- | --- |

**判断死活：** `tmux has-session -t cc-talentpilot`

---

## 小Q Session（SQLite，--resume SESSION_ID 续接）

**规则：**
- 新 topic → 开新 session，登记
- 同一 topic 继续 → `--resume SESSION_ID`，更新时间
- 超过 24h 未用 → stale，开新 session

**当前活跃 Session**

| TOPIC | SESSION_ID | LAST_UPDATED | 状态 |
| --- | --- | --- | --- |
| TalentPilot TEST CASE 审阅 | 20260508_090413_ef3cb4 | 2026-05-08 | 🟢 审阅完成，待更新TESTCASE |

**已完成 Session**

| TOPIC | SESSION_ID | COMPLETED |
| --- | --- | --- |
