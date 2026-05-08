# TalentPilot — 重启恢复状态文件

> 重启前小P保存，系统恢复后按此文件快速拉起服务

**保存时间：** 2026-05-08
**最新 commit：** `e4111b9`（SESSION_TRACKER v0.6 重启保存版，已 push）

---

## Git 状态

- 分支：main
- Working tree：clean（无未提交变更）
- 最新提交：`cccb732` — "docs: TESTCASE v0.5 - E2E核心功能标记通过(14/20)"

---

## 运行中进程（重启后实时）

| 服务 | PID | 端口 | 说明 |
|------|-----|------|------|
| .NET API | 623 | 5010 | TalentPilot Backend |
| Vite 前端 | 665 | 5173 | TalentPilot Frontend |
| MySQL | — | 3306 | talentpilot 数据库（root/Sandvik2026!）|

**状态确认时间：** 2026-05-08 重启后

---

## 重启后启动命令

```bash
# 1. MySQL（一般自动启动，手动确认）
mysql -h 127.0.0.1 -u root -p'Sandvik2026!' -e "SELECT 1" 2>/dev/null && echo "MySQL OK"

# 2. 拉起后端 API（端口5010）
cd /mnt/d/Git/TalentPilot/backend/TalentPilot.Api
dotnet run --urls=http://0.0.0.0:5010 &

# 3. 拉起前端（端口5173）
cd /mnt/d/Git/TalentPilot/frontend
node_modules/.bin/vite --host 0.0.0.0 --port 5173 &

# 4. 验证
curl -s http://localhost:5010/api/health || echo "API未响应"
curl -s http://localhost:5173 | head -5 || echo "前端未响应"
```

---

## 快速验证登录

- URL：http://localhost:5173
- 用户名：`admin`
- 密码：`TalentPilot2026!`

---

## 重要笔记

- TalentNexus 目录已删除（`/mnt/d/Git/TalentNexus` 已清理）
- TalentPilot 是唯一项目，不是 TalentNexus
- MySQL 连接：`Server=127.0.0.1;Port=3306;Database=talentpilot;User=root;Password=Sandvik2026!`
**admin 默认密码：** `TalentPilot2026`（无感叹号）
- CC tmux session：`cc-sandvik`（需手动检查/重启）

---

## 下一步待办

1. 重启后先验证服务正常
2. 确认 T-17 E2E 边缘场景（6项非阻塞问题）
3. Phase 6 方向待 Mark 决策（性能/AI增强/v1.0验收）
