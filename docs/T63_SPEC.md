# T-63: Phase 13 — F-19/F-20体验完善 + F-24体验优化

## 需求来源
PRD §3.5 F-19/F-20, §3.8 F-24

## F-19 已完成（小P）
- ✅ 环境提醒弹窗（进入面试前必须勾选确认）
- ✅ 点击链接即可开始
- ✅ 浏览器+麦克风，支持手机和电脑
- ✅ 随时可退出

## F-20 已实现（隐含）
- ✅ 相同问题集
- ✅ 统一评分标准
- ✅ 技能/逻辑/沟通/真实性评估

## 待CC完成

### 1. F-19: InterviewDuration默认20分钟（当前可能30分钟）
检查并确保默认面试时长为20分钟，不超过30分钟。
- 文件: AIInterviewSessionsController.cs 或 AIInterviewSessionService.cs
- 确保 `InterviewDuration` 默认值 = 20

### 2. F-19: 候选人退出时记录"主动放弃"状态
当前 `CandidateInterview.vue` 已有退出按钮但可能没记录放弃原因。
- 候选人主动关闭页面或点击"退出"时，调用 `PATCH /api/ai-interview-sessions/{id}/abandon`
- 需要在后端添加 `AbandonSession` 端点，设置 `Status = "Abandoned"`
- 确保 `session.InterviewerUserId` 也被正确记录

### 3. F-24: 候选人面试记录页面
为候选人提供一个简单的页面查看自己的面试历史和状态。
- 新页面: `CandidateDashboard.vue` 或在现有页面添加标签页
- 候选人输入会话码查看自己的面试记录（状态、时间、报告链接）
- API: `GET /api/interview-reports/by-session/{sessionId}`（已实现）

### 4. F-24: 面试进度续接（离开后返回）
如果候选人在面试中途关闭页面，再次打开时能续接上次的进度。
- 使用 `localStorage` 保存当前 questionIndex 和已回答的问题
- 重新进入时检测 `localStorage` 中是否有未完成的面试
- 询问"您有未完成的面试，是否继续？"

### 5. F-24: 面试时间提醒（倒计时优化）
- 在倒计时 < 30秒 时，显示红色警告提示
- 添加"还剩30秒"语音或文字提示

## 实现顺序
1. 添加 AbandonSession 端点（后端）
2. 前端退出时调用 abandon
3. InterviewDuration 默认20分钟
4. 候选人仪表板（简单页面）
5. 进度续接 localStorage
6. 倒计时警告优化

## 验收标准
- [ ] InterviewDuration 默认20分钟
- [ ] 候选人退出 → Status=Abandoned
- [ ] 候选人可查看自己的面试历史
- [ ] 关闭页面后返回可续接进度
- [ ] 倒计时<30秒红色警告
- [ ] dotnet build 0 errors
- [ ] npm run build 0 errors
- [ ] commit: "feat: Phase 13 F-19/F-20/F-24 candidate experience"

Work in /mnt/d/Git/TalentPilot.
