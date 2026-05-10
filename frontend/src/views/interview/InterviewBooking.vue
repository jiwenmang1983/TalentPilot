<template>
  <div class="page-header">
    <div class="page-header-left">
      <h1>📅 面试时间预约</h1>
      <p>请选择您的面试时间</p>
    </div>
  </div>

  <div class="booking-container">
    <div class="booking-card">
      <div v-if="loading" class="loading">
        <a-spin size="large" />
        <p>加载中...</p>
      </div>

      <div v-else-if="error" class="error-state">
        <a-result status="error" title="加载失败" :sub-title="error">
          <template #extra>
            <a-button type="primary" @click="$router.push('/login')">返回登录</a-button>
          </template>
        </a-result>
      </div>

      <div v-else-if="success" class="success-section">
        <a-result status="success" title="预约成功">
          <template #subTitle>
            <p>您的面试时间：{{ formatDate(sessionInfo?.scheduledAt) }}</p>
            <p class="tips">我们已将您的预约信息通知招聘方，请保持手机畅通</p>
          </template>
          <template #extra>
            <a-button type="primary" @click="goToInterview">进入面试</a-button>
          </template>
        </a-result>
      </div>

      <div v-else-if="sessionInfo" class="booking-content">
        <div class="company-logo">
          <a-avatar :size="80" style="background-color: #1890ff">
            <template #icon><VideoCameraOutlined /></template>
          </a-avatar>
        </div>

        <h1 class="title">AI 面试预约</h1>
        <p class="company">{{ sessionInfo.jobPostTitle }}</p>

        <a-divider />

        <a-descriptions :column="1" size="small">
          <a-descriptions-item label="应聘职位">
            <strong>{{ sessionInfo.jobPostTitle }}</strong>
          </a-descriptions-item>
          <a-descriptions-item label="面试时长">
            <a-tag color="blue">{{ sessionInfo.interviewDuration || 30 }} 分钟</a-tag>
          </a-descriptions-item>
          <a-descriptions-item v-if="bookingDeadline" label="预约截止时间">
            <a-tag color="red">{{ formatDate(bookingDeadline) }}</a-tag>
          </a-descriptions-item>
        </a-descriptions>

        <a-divider />

        <div class="slots-section">
          <h3>请选择面试时间</h3>
          <div v-if="slotsLoading" class="loading-slots">
            <a-spin size="small" /> 加载可用时间段...
          </div>
          <div v-else class="slots-grid">
            <div v-for="(group, date) in groupedSlots" :key="date" class="date-group">
              <div class="date-header">{{ formatDateHeader(date) }}</div>
              <div class="time-slots">
                <a-button
                  v-for="slot in group"
                  :key="slot.toISOString()"
                  :type="selectedSlot?.toISOString() === slot.toISOString() ? 'primary' : 'default'"
                  :disabled="isSlotPast(slot)"
                  class="time-slot-btn"
                  @click="selectedSlot = slot"
                >
                  {{ formatTime(slot) }}
                </a-button>
              </div>
            </div>
          </div>
        </div>

        <a-divider />

        <div class="action-buttons">
          <a-button
            type="primary"
            size="large"
            :loading="bookingLoading"
            :disabled="!selectedSlot"
            @click="handleBook"
          >
            确认预约
          </a-button>
          <a-button size="large" @click="goBack">返回</a-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { VideoCameraOutlined } from '@ant-design/icons-vue'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import utc from 'dayjs/plugin/utc'
import timezone from 'dayjs/plugin/timezone'

dayjs.extend(relativeTime)
dayjs.extend(utc)
dayjs.extend(timezone)

const route = useRoute()
const router = useRouter()
const loading = ref(true)
const slotsLoading = ref(false)
const bookingLoading = ref(false)
const error = ref(null)
const success = ref(false)
const sessionInfo = ref(null)
const slots = ref([])
const selectedSlot = ref(null)
const bookingDeadline = ref(null)

const groupedSlots = computed(() => {
  const groups = {}
  slots.value.forEach(slot => {
    const dateKey = dayjs(slot).format('YYYY-MM-DD')
    if (!groups[dateKey]) {
      groups[dateKey] = []
    }
    groups[dateKey].push(slot)
  })
  return groups
})

function formatDate(date) {
  if (!date) return ''
  return dayjs(date).format('YYYY-MM-DD HH:mm')
}

function formatDateHeader(date) {
  const d = dayjs(date)
  const today = dayjs().startOf('day')
  const tomorrow = today.add(1, 'day')
  if (d.isSame(today, 'day')) return '今天'
  if (d.isSame(tomorrow, 'day')) return '明天'
  return d.format('MM/DD ddd')
}

function formatTime(date) {
  return dayjs(date).format('HH:mm')
}

function isSlotPast(slot) {
  return dayjs(slot).isBefore(dayjs())
}

async function fetchSessionByToken() {
  const token = route.params.sessionToken
  if (!token) {
    error.value = '无效的预约链接'
    loading.value = false
    return
  }

  try {
    const res = await fetch(`/api/ai-interview-sessions/by-token/${token}`)
    const data = await res.json()
    if (data.success && data.data) {
      sessionInfo.value = data.data
      bookingDeadline.value = data.data.bookingDeadline
      await fetchAvailableSlots(data.data.id)
    } else {
      error.value = data.message || '会话不存在或链接已失效'
    }
  } catch (e) {
    console.error(e)
    error.value = '加载失败，请检查链接是否正确'
  } finally {
    loading.value = false
  }
}

async function fetchAvailableSlots(sessionId) {
  slotsLoading.value = true
  try {
    const res = await fetch(`/api/ai-interview-sessions/${sessionId}/slots`)
    const data = await res.json()
    if (data.success && data.data) {
      slots.value = data.data.slots || []
      if (data.data.bookingDeadline) {
        bookingDeadline.value = data.data.bookingDeadline
      }
      if (data.data.interviewDuration) {
        sessionInfo.value.interviewDuration = data.data.interviewDuration
      }
    }
  } catch (e) {
    console.error(e)
    message.error('加载可用时间段失败')
  } finally {
    slotsLoading.value = false
  }
}

async function handleBook() {
  if (!selectedSlot.value || !sessionInfo.value) {
    message.error('请选择面试时间')
    return
  }

  bookingLoading.value = true
  try {
    const res = await fetch(`/api/ai-interview-sessions/${sessionInfo.value.id}/book`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ slotTime: selectedSlot.value.toISOString() })
    })
    const data = await res.json()
    if (data.success) {
      success.value = true
      sessionInfo.value.scheduledAt = data.data.scheduledAt
      message.success('预约成功')
    } else {
      message.error(data.message || '预约失败')
    }
  } catch (e) {
    console.error(e)
    message.error('预约失败')
  } finally {
    bookingLoading.value = false
  }
}

function goToInterview() {
  router.push(`/interview/candidate?sessionId=${sessionInfo.value.id}`)
}

function goBack() {
  router.push('/login')
}

onMounted(() => {
  fetchSessionByToken()
})
</script>

<style scoped>
.booking-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
}

.booking-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  padding: 40px;
  width: 100%;
  max-width: 600px;
}

.loading {
  text-align: center;
  padding: 60px 0;
}

.loading p {
  margin-top: 16px;
  color: #666;
}

.error-state {
  padding: 20px 0;
}

.booking-content {
  text-align: center;
}

.company-logo {
  margin-bottom: 20px;
}

.title {
  font-size: 28px;
  font-weight: 600;
  color: #333;
  margin: 0;
}

.company {
  font-size: 16px;
  color: #666;
  margin: 8px 0 0;
}

.slots-section {
  text-align: left;
  margin: 24px 0;
}

.slots-section h3 {
  margin-bottom: 16px;
  color: #333;
}

.loading-slots {
  text-align: center;
  padding: 20px;
  color: #666;
}

.slots-grid {
  max-height: 400px;
  overflow-y: auto;
}

.date-group {
  margin-bottom: 20px;
}

.date-header {
  font-weight: 600;
  color: #333;
  margin-bottom: 10px;
  padding: 8px 0;
  border-bottom: 1px solid #f0f0f0;
}

.time-slots {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 8px;
}

.time-slot-btn {
  min-width: 80px;
}

.action-buttons {
  display: flex;
  gap: 16px;
  justify-content: center;
  margin-top: 24px;
}

.action-buttons button {
  min-width: 120px;
}

.success-section {
  padding: 20px 0;
}

.tips {
  color: #888;
  font-size: 14px;
  margin-top: 8px;
}
</style>