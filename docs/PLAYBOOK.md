# TalentPilot Playbook — 运营手册

> 本文件是 TalentPilot 项目的核心协作规则，所有参与者（Mark / 小P / CC / 小Q）共享。
> 维护：小P主动更新，Mark 最终审批。
> 每次规则变更需 Mark 口头确认。

---

## 一、文档体系

### 1.1 文档列表


| 文档 | 用途 | 维护人 | 更新时机 |
| --- | --- | --- | --- |
| PRD.md | 产品需求唯一真相源 | 小P | 需求变更时 |
| WBS.md | 开发任务分解 + 状态追踪 | 小P | 每任务完成时 |
| TESTCASE.md | 测试用例库 + 测试状态表 | 小P | 每测试完成时 |
| PLAYBOOK.md | 本文件，协作规则 | 小P | 规则变更时 |
| SESSION_TRACKER.md | CC 任务委派总账 + 实时状态追踪 | 小P | 每次委派/状态变化时 |


**优先级原则：PRD.md 是所有开发任务的唯一规格来源。其他文档不得与 PRD.md 冲突，冲突时以 PRD.md 为准。**

### 1.2 文档更新触发规则

```
【强制】每完成一个开发/测试任务，小P 主动更新以下文档，不等 Mark 提醒。
【强制】跟 Mark 聊天后，根据聊天记录自动识别需要更新的文档并立即更新。

开发任务完成（CC）
  → WBS.md  T-XXX  🔄→✅
  → PRD.md  同步确认的逻辑（如有）

测试任务完成（小P）
  → TESTCASE.md  ✅/❌
  → WBS.md       测试列更新

跟 Mark 聊天后
  → 识别涉及哪些文档（PRD/WBS/TESTCASE/SESSION_TRACKER/PLAYBOOK）
  → 有变更立即更新，不等下一次
```

### 1.3 联动修改范围

| 本次修改 | 需同步检查 |
| --- | --- |
| PRD.md §X | TESTCASE.md / WBS.md |
| WBS.md T-XXX | PRD.md 相关章节 / TESTCASE.md |
| TESTCASE.md | WBS.md 测试关联 |
| SESSION_TRACKER.md 任务委派更新 | WBS.md / CC任务队列 |
| 任何文档 | 发现前面章节有冲突也一并修正 |

---

## 二、角色与分工

### 2.1 角色定义
```
Mark（吉文）  → 指挥官，最终审批人，所有重大决策
小P（Hermes） → 产品经理 + 调度员，修文档 / 拆需求 / 派任务 / 验收
CC              → 编码开发，小P委派后执行
小Q（slh-bot） → 测试工程师，小P委派后执行（⚠️ 暂不需要，Phase 6 测试由小P负责）
```

### 2.2 分工边界（强制，不跨域）
```
小P 不做                         CC/小Q 不做
────────────────────────         ────────────────────────
写代码                           修文档（PRD/WBS/PLAYBOOK）
直接操作 DB                      自行决定需求逻辑
代替 CC/小Q 做决策               跨域协作（找 Mark 直接要答案）
跳过文档直接开发                  在 PRD 未确认前开始开发

核心原则：小P 修文档派任务，CC/小Q 执行，不交叉混乱。
```

---

## 二.5 SESSION_TRACKER.md 使用规范

### 2.5.1 用途（合并了原 HERMES_TASKS.md）
SESSION_TRACKER.md 是**任务委派总账 + Agent 实时状态**的统一入口：
- **任务委派总账**：所有委派给 CC/小Q 的任务完整历史（替代原 HERMES_TASKS）
- **实时状态**：CC tmux 会话状态、小Q SQLite Session、API 运行状态
- 确保：任何人都能一眼看出谁在干什么、任务不丢、上下文不断

### 2.5.2 CC 状态追踪

| 字段 | 填写规则 |
| --- | --- |
| tmux session | 固定 `cc-talentpilot`，不换 |
| 状态 | 🟢 空闲 / 🟡 进行中 / 🔴 已中断 |
| 当前任务 | 当前执行的任务ID（如 T-09） |
| 最后活动时间 | 每次发任务/收到结果时更新 |

**判断死活：**
```bash
tmux has-session -t cc-talentpilot
# 0 = 活着，1 = 已终止
```

**任务队列状态：**
- 🔴 未开始 → 🟡 进行中 → ✅ 完成
- 每次委派新任务时更新对应行

### 2.5.3 小Q Session 追踪

| 字段 | 填写规则 |
| --- | --- |
| TOPIC | 当前测试/分析的主题 |
| SESSION_ID | slh-bot 返回的 session ID（续接必需） |
| LAST_UPDATED | 每次 resume 后更新 |
| 状态 | 🟢 活跃 / 🟡 待续接 / ⚫ 已结束 |

**续接规则：**
- 同一 topic → 必须带 `--resume SESSION_ID`
- 新 topic → 开新 session，登记进表格
- 超过 24h 未用 → stale，开新 session

**SESSION_ID 获取：**
```bash
RESULT=$(hermes -p slh-bot chat -q "..." --max-turns 5 2>&1)
SESSION_ID=$(echo "$RESULT" | grep "^Session:" | awk '{print $2}')
```

### 2.5.4 更新时机（强制）

```
委派 CC 任务      → 更新 CC 状态行（🟡 进行中）
CC 任务完成       → 更新 CC 状态（🟢 空闲）+ 任务队列（✅）
委派小Q 对话     → 新增 Session 行（🟡）
小Q 对话完成/放弃 → 更新 Session 状态（⚫）
发现 Agent 崩溃   → 记录中断时间 + 原因 + 恢复计划
```

### 2.5.5 API 服务状态

| 字段 | 填写规则 |
| --- | --- |
| 端口 | 当前运行端口（如 5010） |
| 状态 | 🟢 运行中 / 🔴 已停止 |
| 最后验证 | 每次测试后更新 |
| Git commit | 最新 commit SHA |

---

## 三、CC 协作规范

### 3.1 调用方式

```bash
cd /mnt/d/Git/TalentPilot
ANTHROPIC_API_KEY="${AN...KEY}" \
  ANTHROPIC_BASE_URL="https://api.minimaxi.com/anthropic" \
  claude --dangerously-skip-permissions --print \
  -p "任务描述（必须含五段式，见3.2）" \
  --max-turns 99
```

（API Key 具体值见 §3.6）


| 参数 | 作用 |
| --- | --- |
| `--dangerously-skip-permissions` | 跳过每次确认提示，自动化必需 |
| `--print` | 结果输出到 stdout（非交互TTY），小P可捕获 |
| `-p "..."` | 内联任务描述，不依赖文件 |
| `--max-turns N` | 防止无限循环，99步适合大多数任务 |


**CC 步数经验值：**

| 步数 | 适用场景 |
| --- | --- |
| 35步 | 3-4个文件 |
| 50步 | 5-6个文件 |
| 99步 | 7+文件或完整功能（标准值） |
| `ANTHROPIC_BASE_URL` | 必须 = `https://api.minimaxi.com/anthropic`（不是 /v1） |


### 3.2 任务委派五段式（每次必须完整）

```markdown
【背景】
来源：PRD §X.X / T-XXX
问题：当前状态/已知的Bug
目标：交付什么

【约束】
- 已确定的技术决策：...
- 不得修改：Entity结构 / 数据库schema
- 开发环境：...

【目标】
1. ...
2. ...

【验收条件】
- [ ] 构建通过，0 errors
- [ ] API curl 验证通过
- [ ] 结果写入 /tmp/cc_result_<taskid>.json

【环境信息】
工作目录：/mnt/d/Git/TalentPilot/...
API 端口：...
DB：...
认证：...
```

### 3.3 工作流（探索期：CC 直接写 master）

**⚠️ 重要：CC 委派必须用终端后台，不可用 delegate_task**

```
1. 小P 写 prompt 到文件：/tmp/cc_task_<taskid>.txt（五段式，见3.2）
2. 小P 用 terminal(background=True) 启动 CC：
   terminal(background=True,
     command=(
       'cd /mnt/d/Git/TalentPilot && '
       'ANTHROPIC_API_KEY="***" '
       'ANTHROPIC_BASE_URL="https://api.minimaxi.com/anthropic" '
       '/home/markji/.hermes/node/bin/claude '
       '--dangerously-skip-permissions --print '
       '-p "$(cat /tmp/cc_task_<taskid>.txt)" '
       '--max-turns 99 2>&1 | tee /tmp/cc_<taskid>_output.txt'
     ))
3. CC 在 WSL 后台执行（--print 模式，不阻塞 Hermes 主会话）
4. CC 完成后，小P 检查 /tmp/cc_<taskid>_output.txt 验证结果
5. 小P 执行后续步骤：
   a. 构建验证（语言/框架相关）
   b. 数据库更新（如有 migration）
   c. API 服务重启
   d. curl 验证关键接口
   e. git add + commit + push（探索期：直接 push master，不走 PR）
6. 结果写入 /tmp/cc_result_<taskid>.json（可选）
7. 小P 验证，更新文档
```

**追踪 CC 进程：**

```bash
ps aux | grep claude | grep -v grep
# 有输出 = 在跑；无输出 = 已完成（检查 output.txt）
```

**禁止使用的方式：**

- ❌ `delegate_task(acp_command="claude")` — 会被打断，不可用
- ❌ foreground 模式 — 阻塞 Hermes 主会话，导致无法响应用户
- ❌ `cat file | claude -p` — 会产生两个进程

### 3.4 结果文件格式

CC 完成任务后必须写入：

```json
{
  "task_id": "cc_<taskid>",
  "status": "success | failure",
  "build": "pass | fail",
  "api_verification": "pass | fail",
  "files_changed": ["file1", "file2"],
  "commit_sha": "abc1234",
  "notes": "..."
}
```

路径：`/tmp/cc_result_<taskid>.json`

### 3.5 完成标准（Checklist）

```
[ ] 构建通过，0 errors
[ ] 数据库 migration（如有 schema 变更）
[ ] API 服务重启
[ ] curl 验证关键接口
[ ] git commit + push
[ ] 结果写入 /tmp/cc_result_<taskid>.json
```

### 3.6 当前配置

**Provider：** MiniMax CN
**Base URL：** https://api.minimaxi.com/anthropic（⚠️ 不是 /v1）
**Model：** MiniMax-M2.7
**Key：** 见 ccs-admin skill（`sk-cp-AUnpds5n...`）
**Rate Limit 处理：** 429 → 等到下一个整点再试

### 3.7 tmux 交互模式（复杂任务）

> 适用场景：复杂任务需要多轮对话（>15步）、中途需要授权确认、或 CC 主动提问。

**3.7.1 启动 tmux session**

```bash
# 清理并启动命名 tmux 会话
tmux kill-session -t cc-talentpilot 2>/dev/null; sleep 1
tmux new-session -d -s cc-talentpilot
tmux send-keys -t cc-talentpilot \
  'cd /mnt/d/Git/TalentPilot && ANTHROPIC_API_KEY="***" ANTHROPIC_BASE_URL="https://api.minimaxi.com/anthropic" claude --dangerously-skip-permissions --model MiniMax-M2.7' \
  Enter

# 等待 CC 启动（约8s）
sleep 8

# 确认 workspace 信任（发 1 后立即 Enter）
tmux send-keys -t cc-talentpilot '1' Enter
sleep 5
```

**3.7.2 发送任务**

```bash
tmux send-keys -t cc-talentpilot '你的任务描述...' Enter
```

**轮询进度（不等结果，继续并行工作）：**

```bash
tmux capture-pane -t cc-talentpilot -p -S -10 | cat
```

**判断 CC 状态：**

| 底部显示 | 含义 | 操作 |
| --- | --- | --- |
| `❯` + `Thinking`/`Ideating`/`Cooking` 等词 | CC 在思考 | 等待 |
| `❯` 末尾，无特殊词 | CC 空闲，可发新任务 | ✅ 可发任务 |
| `❯` + `esc to interrupt` | CC 长时间等待 | 可中断或发指令 |
| `Agent(…)` + `Done (N tool uses)` | CC 刚完成一步 | 等待或看输出 |

**3.7.3 实时反馈与中断**

```bash
# A. CC 在等输入 → 直接发答案
tmux send-keys -t cc-talentpilot '答案内容' Enter

# B. CC 卡住（>2分钟无响应）→ Ctrl+C 中断后重发
tmux send-keys -t cc-talentpilot C-c
sleep 2
tmux send-keys -t cc-talentpilot '重新描述任务' Enter

# C. 强制终止会话
tmux kill-session -t cc-talentpilot
```

---

## 四、小Q 协作规范

### 4.1 两种调用模式

| 模式 | 命令 | 上下文保持 | 适用场景 |
| --- | --- | --- | --- |
| **对话模式（§4.2）** | `hermes -p slh-bot chat -q "..." --resume SESSION_ID` | ✅ SQLite session 续接 | 需要 LLM 推理的复杂测试分析 |
| **脚本模式（§4.3）** | `python3 /tmp/q_test.py` | N/A | 标准 API 测试，无需 LLM 参与 |

### 4.1.1 整体 Agent 通讯架构

```
Mark（吉文）
  └── 飞书 DM → Hermes（小P，我）
                       ├── tmux send-keys → CC（cc-talentpilot）
                       │                   全程同一 tmux session，上下文不中断
                       │
                       ├── hermes -p slh-bot chat -q → 小Q（slh-bot）
                       │   --resume SESSION_ID   ← 必须带，续接 SQLite session 上下文
                       │   两条路径：
                       │     ① 对话模式（§4.2）→ LLM 推理分析
                       │     ② 脚本模式（§4.3）→ python3 直接跑
                       │
                       └── CC 和小Q 并行，互不等待
```

**调度原则：**
- Hermes 同时只调度一个任务给 CC，一个任务给小Q
- CC 和小Q 并行执行，互不等待
- Hermes 发完指令后继续处理 Mark 的其他消息，不需要等 slh-bot 返回
- **小Q调用必须带 `--resume SESSION_ID`**

### 4.2 对话模式（session resume）

**Step 1：发起第一次对话，捕获 session ID**

```bash
RESULT=$(hermes -p slh-bot chat \
  -q "执行以下测试分析：..." \
  --max-turns 10 2>&1)
echo "$RESULT"

# 从输出中提取 session ID：
SESSION_ID=$(echo "$RESULT" | grep "^Session:" | awk '{print $2}')
```

**Step 2：续接同一 session 继续对话**

```bash
RESULT=$(hermes -p slh-bot chat \
  -q "TC-0302 失败了，分析原因：..." \
  --resume "$SESSION_ID" \
  --max-turns 10 2>&1)
echo "$RESULT"
```

### 4.3 脚本模式（API 测试）

适用场景：标准化的 API 测试用例（TC-XXXX），脚本内部完成认证+测试+断言。

```bash
python3 /tmp/q<NNN>_test.py 2>&1 | tee /tmp/q<NNN>_result.txt
```

**脚本规范：**
- 文件名：`/tmp/q<NNN>_test.py`（Q-XXX 任务编号）
- 最后一行打印：`PASS` / `FAIL` / `Q-NNN COMPLETE`

### 4.4 测试完成标准

```
【Q-FW 框架探测】
[ ] 所有端点返回 200/201（无 404，无 500）
[ ] Bug 记录同步到 ISSUE_LOG

【Q-XXX 功能 E2E】
[ ] 所有 TC PASS
[ ] TESTCASE.md Q-XXX 状态更新
```

---

## 五、代码提交规范

### 5.1 分支策略

**当前阶段（探索期）：** CC 直接写 master，不走 PR 流程。

**远期阶段（规范期）：**
- `feat/<功能简述>` 分支开发 → commit → push → PR → Mark Review → 合并

### 5.2 Push 规则

```
默认：普通 git push（不用 force-push）
Force-push（如清理垃圾 commit）：必须先发飞书 DM 给 Mark 审批，拿到确认后才能执行
```

### 5.3 Commit 规范

```
格式：<type>: <简短描述>
type：feat | fix | docs | refactor | test | chore
示例：
  feat: add resume collection API
  fix: resolve matching logic edge case
  docs: update PRD with interview flow
```

---

## 六、问题升级路径

```
1. Migration apply 了没有？（如有 DB migration 的项目）
2. API 服务启动了没有？
3. 认证 token 有效吗？
4. CC rate limit？
   → MiniMax 429 → 等下一个整点刷新
```

---

*本文档由 Hermes 小P 维护，Mark 最终审批。*
