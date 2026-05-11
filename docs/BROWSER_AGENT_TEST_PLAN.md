# Browser Agent 明天测试计划

## 测试目标
端到端验证 Browser Agent 采集流程：Chrome Debug → 手动登录 Boss → 保存 Session → 按需采集简历

---

## 第一步：Windows 启动 Chrome Debug
**（在 Windows CMD 或 PowerShell 中运行，不要在 WSL 里）**

```bash
chrome.exe --remote-debugging-port=9222 --user-data-dir="C:\Users\<你的Windows用户名>\chrome-debug"
```

> ⚠️ 把 `<你的Windows用户名>` 换成实际用户名（比如 `jiwen`）
> ⚠️ 如果 `chrome.exe` 不在 PATH，可以直接用完整路径，比如：
> `"C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222 --user-data-dir="C:\Users\jiwen\chrome-debug"`

---

## 第二步：手动登录 Boss 直聘
1. Chrome Debug 窗口打开后，手动访问 https://www.zhipin.com
2. 用账号密码登录 Boss（滑动验证码需要人工完成）
3. 登录成功后，不要关闭这个 Chrome 窗口

---

## 第三步：保存 Session（WSL 终端）

```bash
# 获取 token
TOKEN=$(curl -s -X POST http://localhost:5010/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"TalentPilot2026"}' | \
  python3 -c "import sys,json; print(json.load(sys.stdin)['data']['accessToken'])")

# 保存 session
curl -X POST "http://localhost:5010/api/browser-agent/save-session" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"platform":"boss"}'

# 验证
curl -s -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5010/api/browser-agent/login-status"
```

**预期输出**：`isValid: true` 或 `status: LoggedIn`

---

## 第四步：运行一次采集（选一个 jobPostId）

```bash
TOKEN=$(curl -s -X POST http://localhost:5010/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"TalentPilot2026"}' | \
  python3 -c "import sys,json; print(json.load(sys.stdin)['data']['accessToken'])")

# 查看可用职位
curl -s -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5010/api/browser-agent/job-posts" | \
  python3 -c "import sys,json; d=json.load(sys.stdin); [print(f'ID: {i[\"id\"]} - {i[\"title\"]}') for i in d['data']['items']]"

# 用第一个职位采集（把 13 换成实际 ID）
curl -X POST "http://localhost:5010/api/browser-agent/collect" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"jobPostId": 13}'
```

**预期输出**：
- `status: "Success"` 或 `"Collecting"`
- `totalCandidates: N`（采集到的候选人数）

---

## 第五步：验证简历入库

```bash
# 方式1：查数据库
mysql -h 127.0.0.1 -u root -pSandvik2026! talentpilot \
  -e "SELECT id, name, source, created_at FROM resumes ORDER BY id DESC LIMIT 10;"

# 方式2：查 API（如果 ResumeController 有 list 端点）
curl -s -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5010/api/resumes?page=1&pageSize=10" | \
  python3 -m json.tool
```

---

## 一行命令验证所有端点（快速冒烟测试）

```bash
# 完整冒烟测试
TOKEN=$(curl -s -X POST http://localhost:5010/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"TalentPilot2026"}' | \
  python3 -c "import sys,json; print(json.load(sys.stdin)['data']['accessToken'])")

echo "=== 1. Connection ==="
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:5010/api/browser-agent/connection"
echo ""
echo "=== 2. Login Status ==="
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:5010/api/browser-agent/login-status"
echo ""
echo "=== 3. Job Posts ==="
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:5010/api/browser-agent/job-posts" | python3 -c "import sys,json; d=json.load(sys.stdin); print(f'Total: {d[\"data\"][\"total\"]} jobs')"
echo ""
echo "=== 4. Collect (jobPostId=13) ==="
curl -s -X POST "http://localhost:5010/api/browser-agent/collect" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"jobPostId": 13}' | python3 -c "import sys,json; d=json.load(sys.stdin); print(json.dumps(d, indent=2, ensure_ascii=False)[:500])"
```

---

## 常见问题排查

| 问题 | 排查 |
|------|------|
| `connected: false` | Chrome Debug 没启动，或端口 9222 被防火墙拦截 |
| `NeedsManualLogin` | Boss cookies 未保存，先 `/save-session` |
| 采集返回空 | Boss 搜索结果页加载失败，试试 `/screenshot` 看实际页面 |
| WSL 连不上 Windows :9222 | 确认 Chrome 启动时用的是 `localhost:9222` 不是 `127.0.0.1:9222` |
| `No service for type` | 后端没重启，用的旧代码 |

---

## MiniMax API 验证（独立测试 Vision）

```bash
# 测试 MiniMax API 是否正常（跟 Browser Agent 分开测）
curl -s -X POST https://api.minimaxi.com/anthropic/v1/messages \
  -H "x-api-key: $MINIMAX_API_KEY" \
  -H "anthropic-version: 2023-06-01" \
  -H "Content-Type: application/json" \
  -d '{"model":"MiniMax-M2.7","max_tokens":10,"messages":[{"role":"user","content":"Say hi in 3 words"}]}' | \
  python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('content',[{}])[0].get('text','NO_TEXT'))"
```

---

## 测试完成标准

- [ ] `/connection` → `connected: true`
- [ ] `/login-status` → `isValid: true`
- [ ] `/collect` → 不返回 `NeedsManualLogin`
- [ ] resumes 表有新增记录
- [ ] `totalCandidates > 0`
