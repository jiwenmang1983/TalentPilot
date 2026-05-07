# TalentPilot Contributing Guide

> 本项目（TalentPilot）所有开发人员必须遵循本文档规范。
> 仓库：`/mnt/d/Git/TalentPilot` | 分支：`main`

---

## 1. 团队角色

| 角色 | 负责人 | 职责 |
| --- | --- | --- |
| **最终审批人** | 吉文（Mark） | 确认并批准合并 |
| **PM / Agent** | 小P | 调度 CC 开发，Review 后提交 PR |
| **测试** | 小Q | 测试工程师，由小P调度执行功能测试 |

---

## 2. 文档体系

| 文档 | 说明 | 维护人 |
| --- | --- | --- |
| `docs/PRD.md` | 产品需求文档（Source of Truth） | Mark + 小P |
| `docs/WBS.md` | 开发任务工作分解结构 | 小P |
| `docs/TESTCASE.md` | 测试用例套件 | 小P |
| `PLAYBOOK.md` | 团队协作规范 | 小P |
| `CONTRIBUTING.md` | 开发规范（本文件） | 小P |

---

## 3. 分支与 Commit 规范

**分支命名：**
```
feat/<功能简述>
fix/<问题简述>
docs/<文档类型>
```

**Commit 格式：**
```
<type>: <简短描述>

type: feat | fix | docs | refactor | test | chore
```

---

## 4. PR 流程

```
分支开发 → commit → push → 创建 PR → Review → Mark 确认 → 合并
```

**当前阶段（探索期）：** CC 直接写 master，不走 PR 流程。

### Push 规则

- **默认使用普通 `git push`**，不用 force-push
- 如需 force-push，必须先发飞书 DM 给 Mark 审批

---

## 5. PRD 驱动开发模式

**核心原则：**
- `docs/PRD.md` — 产品需求文档，**需求功能的唯一事实来源**
- `docs/WBS.md` — 工作分解结构，**开发任务的触发和追踪入口**

### WBS 任务状态

```
🔄 待开发 → 🔄 开发中 → ✅ 已验收 → ✅ 已合并
```

---
