<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>🎤 AI面试</h1>
      <p>AI语音面试进行中</p>
    </div>
  </div>

  <div class="candidate-interview">
    <!-- Token Input Screen -->
    <div v-if="!sessionValid" class="token-screen">
      <div class="token-card">
        <div class="logo">
          <a-avatar :size="80" style="background-color: #1890ff">
            <template #icon><VideoCameraOutlined /></template>
          </a-avatar>
        </div>
        <h1 class="title">AI智能面试</h1>
        <p class="subtitle">请输入您的面试会话码开始面试</p>

        <a-form :model="tokenForm" layout="vertical" class="token-form">
          <a-form-item label="会话码" required>
            <a-input
              v-model:value="tokenForm.token"
              placeholder="请输入会话码"
              size="large"
            />
          </a-form-item>
        </a-form>

        <a-button type="primary" size="large" block :loading="verifying" @click="verifyToken">
          开始面试
        </a-button>
      </div>
    </div>

    <!-- Interview Screen -->
    <div v-else class="interview-screen">
      <!-- Environment Reminder Modal (F-19) -->
      <a-modal
        :open="showEnvironmentModal"
        title="📋 面试环境提醒"
        :footer="null"
        :closable="false"
        :maskClosable="false"
        centered
        width="420px"
      >
        <div class="environment-tips">
          <a-alert
            message="请在开始前确认以下环境要求"
            type="info"
            show-icon
            style="margin-bottom: 16px"
          />
          <ul class="tips-list">
            <li>🎤 请在安静、无干扰的环境中进行面试</li>
            <li>📱 推荐使用手机或电脑，确保麦克风正常工作</li>
            <li>🌐 请确保网络连接稳定</li>
            <li>🚫 面试过程中请勿切换页面或打开其他应用</li>
            <li>⏱️ 面试时长约20-30分钟，请预留充足时间</li>
          </ul>
          <a-checkbox v-model:checked="environmentChecked" style="margin-top: 16px">
            我已阅读并确认环境符合要求
          </a-checkbox>
        </div>
        <div style="margin-top: 20px; text-align: right">
          <a-button @click="showEnvironmentModal = false" style="margin-right: 8px">稍后再说</a-button>
          <a-button type="primary" :disabled="!environmentChecked" @click="confirmEnvironmentAndStart">
            开始面试 ✓
          </a-button>
        </div>
      </a-modal>
      <!-- Header -->
      <div class="interview-header">
        <div class="candidate-info">
          <span class="name">{{ session.candidateName }}</span>
          <span class="position">{{ session.jobPostTitle }}</span>
        </div>
        <div class="progress-info">
          <span>第 {{ currentProgress }} / {{ totalQuestions }} 题</span>
          <a-progress
            :percent="Math.round((currentProgress / totalQuestions) * 100)"
            :show-info="false"
            size="small"
            style="width: 120px"
          />
          <a-button type="text" danger size="small" @click="confirmAbandon" title="退出面试">
            <template #icon><CloseCircleOutlined /></template>
            退出
          </a-button>
        </div>
      </div>

      <!-- Question Area -->
      <div class="question-area" v-if="currentQuestion && !interviewCompleted">
        <div class="countdown-circle">
          <a-progress
            type="circle"
            :percent="countdownPercent"
            :format="() => countdown"
            :stroke-color="countdownColor"
            :width="100"
          />
        </div>

        <div class="question-text">
          <h2 class="question-label">当前问题</h2>
          <p class="question-content">{{ currentQuestion.questionText }}</p>
          <p v-if="currentQuestion.tips" class="question-tips">
            <InfoCircleOutlined /> {{ currentQuestion.tips }}
          </p>
        </div>

        <!-- TTS Audio Player -->
        <div v-if="questionAudioUrl" class="audio-player">
          <a-button type="primary" shape="round" @click="playQuestionAudio" :loading="playingAudio">
            <template #icon><PlayCircleOutlined /></template>
            {{ playingAudio ? '播放中...' : '播放题目' }}
          </a-button>
        </div>

        <!-- Voice Recorder Area -->
        <div class="voice-recorder">
          <div class="record-status" v-if="isRecording || recordedAnswer">
            <span v-if="isRecording" class="recording-indicator">
              <span class="pulse"></span>
              录音中 {{ recordingTime }}s
            </span>
            <span v-else-if="recordedAnswer" class="recorded-text">
              已录制的回答：{{ recordedAnswer }}
            </span>
          </div>

          <button
            class="record-btn"
            :class="{ recording: isRecording, disabled: submitting || playingAudio }"
            @mousedown="startRecording"
            @mouseup="stopRecording"
            @mouseleave="stopRecording"
            :disabled="submitting || playingAudio"
          >
            <span v-if="!isRecording" class="mic-icon">🎤</span>
            <span v-else class="stop-icon">🔴</span>
            <span class="record-label">
              {{ isRecording ? '松开结束' : (submitting ? '提交中...' : '按住说话') }}
            </span>
          </button>

          <p class="record-hint">按住按钮开始说话，松开自动提交</p>

          <!-- Submit button for re-recording -->
          <div v-if="recordedAnswer && !isRecording" class="re-record-area">
            <a-button @click="clearRecording" :disabled="submitting">重新录制</a-button>
            <a-button
              type="primary"
              :loading="submitting"
              @click="submitVoiceAnswer"
              :disabled="!recordedAnswer.trim()"
            >
              提交回答
            </a-button>
          </div>
        </div>
      </div>

      <!-- Completed Screen -->
      <div v-else-if="interviewCompleted" class="completed-screen">
        <a-result
          status="success"
          title="面试已完成"
          :sub-title="`感谢您的参与，您已完成全部 ${totalQuestions} 道问题。`"
        >
          <template #extra>
            <p class="completed-tips">招聘团队将尽快审核您的面试记录</p>
          </template>
        </a-result>
      </div>

      <!-- Loading Next Question -->
      <div v-else-if="loadingQuestion" class="loading-screen">
        <a-spin size="large" />
        <p>加载下一题中...</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { VideoCameraOutlined, InfoCircleOutlined, PlayCircleOutlined, CloseCircleOutlined } from '@ant-design/icons-vue'
import { aiInterviewSessionApi } from '@/api/aiInterviewSession'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'

const route = useRoute()
const tokenForm = ref({ token: '' })
const verifying = ref(false)
const sessionValid = ref(false)
const session = ref(null)
const sessionId = ref(null)

const currentQuestion = ref(null)
const submitting = ref(false)
const loadingQuestion = ref(false)
const interviewCompleted = ref(false)
const currentProgress = ref(0)
const totalQuestions = ref(5)

// Voice recording state
const isRecording = ref(false)
const recordingTime = ref(0)
const recordedAnswer = ref('')
const questionAudioUrl = ref('')
const playingAudio = ref(false)
let mediaRecorder = null
let audioChunks = []
let recordingTimer = null

const countdown = ref(0)
const countdownTimer = ref(null)
const TOTAL_TIME = 120

const countdownPercent = computed(() => {
  return Math.round((countdown.value / TOTAL_TIME) * 100)
})

const countdownColor = computed(() => {
  if (countdown.value < 30) return '#ff4d4f'
  if (countdown.value < 60) return '#faad14'
  return '#1890ff'
})

const showEnvironmentModal = ref(false)
const environmentChecked = ref(false)

async function verifyToken() {
  const token = tokenForm.value.token.trim() || route.query.token
  if (!token) {
    message.error('请输入会话码')
    return
  }

  verifying.value = true
  try {
    const res = await aiInterviewSessionApi.getByToken(token)
    if (res.success && res.data) {
      session.value = res.data
      sessionId.value = res.data.id

      if (res.data.status === 'Pending') {
        await aiInterviewSessionApi.joinByToken(token)
        session.value.status = 'InProgress'
      }

      if (res.data.status === 'InProgress' || res.data.status === 'Pending') {
        // Show environment reminder before starting
        showEnvironmentModal.value = true
      } else {
        message.error('该面试会话状态不允许开始')
      }
    } else {
      message.error(res.message || '无效的会话码')
    }
  } catch (e) {
    message.error('会话码验证失败，请检查后重试')
  } finally {
    verifying.value = false
  }
}

function confirmEnvironmentAndStart() {
  showEnvironmentModal.value = false
  sessionValid.value = true
  loadNextQuestion()
}

function confirmAbandon() {
  if (confirm('确定要退出面试吗？退出后您的面试将被标记为"主动放弃"。')) {
    abandonSession()
  }
}

async function abandonSession() {
  try {
    if (sessionId.value) {
      await aiInterviewSessionApi.abandon(sessionId.value)
    }
  } catch (e) {
    // Best effort
  }
  stopCountdown()
  sessionValid.value = false
  interviewCompleted.value = false
  session.value = null
  sessionId.value = null
  currentQuestion.value = null
  message.info('您已退出面试')
}

async function loadNextQuestion() {
  loadingQuestion.value = true
  questionAudioUrl.value = ''
  recordedAnswer.value = ''
  try {
    const res = await aiInterviewSessionApi.getNextQuestion(sessionId.value)
    if (res.success && res.data?.questionId) {
      currentQuestion.value = res.data
      countdown.value = res.data.timeLimit || TOTAL_TIME
      startCountdown()

      // Try to get TTS audio for the question
      await loadQuestionAudio(res.data.questionId)
    } else {
      interviewCompleted.value = true
      stopCountdown()
    }
  } catch (e) {
    message.error('获取问题失败')
  } finally {
    loadingQuestion.value = false
  }
}

async function loadQuestionAudio(questionId) {
  try {
    const res = await aiInterviewSessionApi.getQuestionAudio(sessionId.value, questionId)
    if (res) {
      const blob = new Blob([res], { type: 'audio/mp3' })
      questionAudioUrl.value = URL.createObjectURL(blob)
    }
  } catch (e) {
    // TTS failed, continue without audio - this is expected
    questionAudioUrl.value = ''
  }
}

async function playQuestionAudio() {
  if (!questionAudioUrl.value || playingAudio.value) return

  playingAudio.value = true
  try {
    const audio = new Audio(questionAudioUrl.value)
    await audio.play()
    audio.onended = () => {
      playingAudio.value = false
    }
    audio.onerror = () => {
      playingAudio.value = false
    }
  } catch (e) {
    playingAudio.value = false
  }
}

async function startRecording() {
  if (isRecording.value || submitting.value || playingAudio.value) return

  try {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
    audioChunks = []
    mediaRecorder = new MediaRecorder(stream, { mimeType: 'audio/webm' })

    mediaRecorder.ondataavailable = (event) => {
      if (event.data.size > 0) {
        audioChunks.push(event.data)
      }
    }

    mediaRecorder.start()
    isRecording.value = true
    recordingTime.value = 0

    // Start timer
    recordingTimer = setInterval(() => {
      recordingTime.value++
    }, 1000)
  } catch (e) {
    message.error('无法访问麦克风，请检查权限设置')
  }
}

async function stopRecording() {
  if (!isRecording.value || !mediaRecorder) return

  clearInterval(recordingTimer)
  recordingTimer = null

  return new Promise((resolve) => {
    mediaRecorder.onstop = async () => {
      isRecording.value = false

      if (audioChunks.length === 0) {
        resolve()
        return
      }

      const audioBlob = new Blob(audioChunks, { type: 'audio/webm' })

      // For now, store a placeholder transcript since we don't have transcription
      // In production, you'd send this to a speech-to-text service
      recordedAnswer.value = `[语音回答 ${recordingTime.value}秒]`

      // Auto-submit after short delay
      setTimeout(async () => {
        await submitVoiceAnswer()
        resolve()
      }, 500)
    }

    mediaRecorder.stop()
    mediaRecorder.stream.getTracks().forEach(track => track.stop())
  })
}

function clearRecording() {
  recordedAnswer.value = ''
  audioChunks = []
}

async function submitVoiceAnswer() {
  if (!recordedAnswer.value.trim() && audioChunks.length === 0) return

  submitting.value = true
  stopCountdown()

  try {
    const audioBase64 = audioChunks.length > 0
      ? await blobToBase64(new Blob(audioChunks, { type: 'audio/webm' }))
      : null

    const res = await aiInterviewSessionApi.submitAnswer(sessionId.value, {
      questionId: currentQuestion.value?.questionId || 'Q0',
      answer: recordedAnswer.value || '[语音回答]',
      audioUrl: audioBase64 ? `data:audio/webm;base64,${audioBase64}` : null
    })

    if (res.success) {
      currentProgress.value = res.data?.progress || currentProgress.value + 1
      audioChunks = []
      recordedAnswer.value = ''
      questionAudioUrl.value = ''

      if (res.data?.nextQuestion) {
        setTimeout(async () => {
          currentQuestion.value = res.data.nextQuestion
          countdown.value = res.data.nextQuestion.timeLimit || TOTAL_TIME
          startCountdown()
          await loadQuestionAudio(res.data.nextQuestion.questionId)
        }, 1000)
      } else {
        interviewCompleted.value = true
      }
    }
  } catch (e) {
    message.error('提交失败，请重试')
    startCountdown()
  } finally {
    submitting.value = false
  }
}

function blobToBase64(blob) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onloadend = () => {
      const base64 = reader.result.split(',')[1]
      resolve(base64)
    }
    reader.onerror = reject
    reader.readAsDataURL(blob)
  })
}

function startCountdown() {
  stopCountdown()
  countdownTimer.value = setInterval(() => {
    if (countdown.value > 0) {
      countdown.value--
    } else {
      handleTimeUp()
    }
  }, 1000)
}

function stopCountdown() {
  if (countdownTimer.value) {
    clearInterval(countdownTimer.value)
    countdownTimer.value = null
  }
}

function handleTimeUp() {
  stopCountdown()
  if (recordedAnswer.value.trim()) {
    submitVoiceAnswer()
  } else {
    message.warning('答题时间已到')
    submitVoiceAnswer()
  }
}

onMounted(() => {
  const urlToken = route.query.token
  if (urlToken) {
    tokenForm.value.token = urlToken
    verifyToken()
  }

  // Handle browser tab close - call abandon if in progress
  window.addEventListener('beforeunload', handleBeforeUnload)
})

function handleBeforeUnload() {
  if (sessionId.value && sessionValid.value && !interviewCompleted.value) {
    abandonSession()
  }
}

onUnmounted(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload)
  stopCountdown()
  if (recordingTimer) clearInterval(recordingTimer)
  if (mediaRecorder && isRecording.value) {
    mediaRecorder.stream.getTracks().forEach(track => track.stop())
  }
  if (questionAudioUrl.value) {
    URL.revokeObjectURL(questionAudioUrl.value)
  }
})
</script>

<style scoped>
.candidate-interview {
  min-height: 100vh;
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
}

.token-screen {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.token-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  padding: 40px;
  width: 100%;
  max-width: 420px;
  text-align: center;
}

.logo {
  margin-bottom: 24px;
}

.title {
  font-size: 28px;
  font-weight: 600;
  color: #333;
  margin: 0 0 8px;
}

.subtitle {
  font-size: 14px;
  color: #666;
  margin: 0 0 32px;
}

.token-form {
  text-align: left;
  margin-bottom: 24px;
}

.interview-screen {
  max-width: 800px;
  margin: 0 auto;
  padding: 24px;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.interview-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: rgba(255, 255, 255, 0.1);
  padding: 16px 24px;
  border-radius: 12px;
  margin-bottom: 32px;
}

.candidate-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.candidate-info .name {
  font-size: 18px;
  font-weight: 600;
  color: white;
}

.candidate-info .position {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.7);
}

.progress-info {
  display: flex;
  align-items: center;
  gap: 12px;
  color: white;
  font-size: 14px;
}

.question-area {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 32px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 16px;
}

.countdown-circle {
  margin-bottom: 32px;
}

.question-text {
  text-align: center;
  margin-bottom: 32px;
  width: 100%;
}

.question-label {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.6);
  text-transform: uppercase;
  letter-spacing: 2px;
  margin-bottom: 16px;
}

.question-content {
  font-size: 24px;
  font-weight: 500;
  color: white;
  line-height: 1.6;
  margin: 0;
}

.question-tips {
  margin-top: 12px;
  font-size: 14px;
  color: rgba(255, 255, 255, 0.5);
}

.audio-player {
  margin-bottom: 24px;
}

.voice-recorder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  width: 100%;
  max-width: 400px;
}

.record-status {
  min-height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.recording-indicator {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #ff4d4f;
  font-weight: 500;
}

.pulse {
  width: 12px;
  height: 12px;
  background: #ff4d4f;
  border-radius: 50%;
  animation: pulse 1s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.5; transform: scale(1.2); }
}

.recorded-text {
  color: rgba(255, 255, 255, 0.8);
  font-size: 14px;
}

.record-btn {
  width: 120px;
  height: 120px;
  border-radius: 50%;
  border: none;
  background: linear-gradient(135deg, #1890ff 0%, #096dd9 100%);
  color: white;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: all 0.3s ease;
  box-shadow: 0 8px 32px rgba(24, 144, 255, 0.4);
}

.record-btn:hover:not(.disabled) {
  transform: scale(1.05);
  box-shadow: 0 12px 40px rgba(24, 144, 255, 0.5);
}

.record-btn.recording {
  background: linear-gradient(135deg, #ff4d4f 0%, #cf1322 100%);
  box-shadow: 0 8px 32px rgba(255, 77, 79, 0.4);
  animation: pulse-shadow 1s ease-in-out infinite;
}

@keyframes pulse-shadow {
  0%, 100% { box-shadow: 0 8px 32px rgba(255, 77, 79, 0.4); }
  50% { box-shadow: 0 8px 48px rgba(255, 77, 79, 0.6); }
}

.record-btn.disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.mic-icon, .stop-icon {
  font-size: 32px;
}

.record-label {
  font-size: 14px;
  font-weight: 500;
}

.record-hint {
  color: rgba(255, 255, 255, 0.5);
  font-size: 12px;
}

.re-record-area {
  display: flex;
  gap: 12px;
  margin-top: 8px;
}

.loading-screen {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 16px;
  color: rgba(255, 255, 255, 0.7);
}

.completed-screen {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.completed-tips {
  color: rgba(255, 255, 255, 0.7);
  font-size: 14px;
}
</style>