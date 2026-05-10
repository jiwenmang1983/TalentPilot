<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>🎤 AI面试</h1>
      <p>AI视频面试进行中</p>
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

        <!-- Answer Input -->
        <div class="answer-area">
          <a-textarea
            v-model:value="answerText"
            placeholder="请输入您的回答..."
            :rows="6"
            :disabled="submitting"
          />
          <a-button
            type="primary"
            size="large"
            :loading="submitting"
            @click="submitAnswer"
            :disabled="!answerText.trim()"
          >
            提交回答
          </a-button>
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
import { VideoCameraOutlined, InfoCircleOutlined } from '@ant-design/icons-vue'
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
const answerText = ref('')
const submitting = ref(false)
const loadingQuestion = ref(false)
const interviewCompleted = ref(false)
const currentProgress = ref(0)
const totalQuestions = ref(5)

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
        sessionValid.value = true
        await loadNextQuestion()
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

async function loadNextQuestion() {
  loadingQuestion.value = true
  try {
    const res = await aiInterviewSessionApi.getNextQuestion(sessionId.value)
    if (res.success && res.data?.questionId) {
      currentQuestion.value = res.data
      countdown.value = res.data.timeLimit || TOTAL_TIME
      startCountdown()
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
  if (answerText.value.trim()) {
    submitAnswer()
  } else {
    message.warning('答题时间已到，请提交空白回答')
    submitAnswer()
  }
}

async function submitAnswer() {
  if (!answerText.value.trim() && !currentQuestion.value) return

  submitting.value = true
  stopCountdown()

  try {
    const res = await aiInterviewSessionApi.submitAnswer(sessionId.value, {
      questionId: currentQuestion.value?.questionId || 'Q0',
      answer: answerText.value.trim(),
      audioUrl: null
    })

    if (res.success) {
      currentProgress.value = res.data?.progress || currentProgress.value + 1
      answerText.value = ''

      if (res.data?.nextQuestion) {
        setTimeout(async () => {
          currentQuestion.value = res.data.nextQuestion
          countdown.value = res.data.nextQuestion.timeLimit || TOTAL_TIME
          startCountdown()
        }, 3000)
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

onMounted(() => {
  const urlToken = route.query.token
  if (urlToken) {
    tokenForm.value.token = urlToken
    verifyToken()
  }
})

onUnmounted(() => {
  stopCountdown()
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

.answer-area {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.answer-area :deep(.ant-input) {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.2);
  color: white;
  font-size: 16px;
}

.answer-area :deep(.ant-input::placeholder) {
  color: rgba(255, 255, 255, 0.4);
}

.answer-area :deep(.ant-btn-primary) {
  align-self: flex-end;
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
