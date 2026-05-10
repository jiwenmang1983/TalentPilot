# T-59: F-18 实时面试流程

## 需求来源
PRD §3.6 F-18：实时面试流程

## 需求描述
候选人进入面试房间后，以语音问答形式完成面试：AI读题(TTS) → 候选人说答案(录音+转写) → 循环直到所有问题完成 → AI结束语。

## 现有状态
- CandidateInterview.vue：基于textarea文本输入，**需要改造成语音界面**
- aiInterviewSession.js：joinByToken/start/complete/submitAnswer/nextQuestion API已存在
- AIInterviewSessionService：nextQuestion返回模拟问题，submitAnswer保存transcript
- 无TTS集成、无录音功能

## 实现方案

### 前端改造（CandidateInterview.vue）

**1. 录音按钮**
- 候选人点击"开始回答"按钮 → 请求麦克风权限 → MediaRecorder录音
- 录音中显示波形动画 + 时长计时
- 松开/再次点击 → 停止录音，上传音频文件 + 调用submitAnswer

**2. AI读题（TTS）**
- 每次显示新问题时，自动调用后端TTS接口获取音频URL
- 前端用Audio组件播放，播放完再开始录音

**3. 面试流程**
```
verifyToken → joinByToken → loadNextQuestion
  → AI读题(TTS播放) → 候选人录音(RecorderJS) → submitAnswer(含transcript)
  → loadNextQuestion → ... → 完成
```

**4. 新API端点**
- `GET /api/ai-interview-sessions/{id}/question-audio/{questionId}` → TTS音频流或URL

### 后端改动

**1. TTS服务集成**
- 复用现有MiniMax API做TTS
- `TtsService.cs`：text → audio_url/bytes
- 使用azure speech或MiniMax语音合成

**2. submitAnswer扩展**
- 接受multipart/form-data：answerText + audioFile
- 存储audioUrl到transcript entry

**3. nextQuestion扩展**
- 返回结构化问题列表（包含questionText + questionId）
- 首次调用返回AI开场白

## 验收标准
- [ ] 候选人进入房间后，AI自动朗读第一道题
- [ ] 候选人按住录音键说话，松开自动上传+转写
- [ ] 所有问题完成后显示"面试已完成"
- [ ] 后端正确存储transcript（含answerText和audioUrl）
- [ ] build 0 errors

## 技术约束
- 浏览器录音：navigator.mediaDevices.getUserMedia + MediaRecorder
- TTS：后端调用MiniMax/TTS API返回音频URL
- 无需WebRTC（语音问答，无需视频通话）
- 候选人端只需Chrome/Edge/Safari+麦克风
