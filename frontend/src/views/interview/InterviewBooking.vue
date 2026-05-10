<template>
  <div class="booking-container">
    <a-card class="booking-card">
      <template #title>
        <span>面试预约</span>
      </template>

      <!-- Loading -->
      <a-spin v-if="loading" tip="加载中..." />

      <!-- Error -->
      <a-result v-if="error" status="error" :title="error" />

      <!-- Booking Form -->
      <div v-if="!loading && !error && !booked">
        <a-typography-paragraph>
          <strong>职位：</strong>{{ sessionInfo.jobPostTitle || '面试' }}
        </a-typography-paragraph>
        <a-typography-paragraph>
          <strong>面试时长：</strong>{{ sessionInfo.interviewDuration || 30 }} 分钟
        </a-typography-paragraph>
        <a-divider>选择面试时间</a-divider>

        <!-- Date tabs -->
        <a-tabs v-model:activeKey="selectedDate">
          <a-tab-pane v-for="date in Object.keys(groupedSlots)" :key="date" :tab="formatDateLabel(date)">
            <a-radio-group v-model:value="selectedSlot" class="slot-group">
              <a-radio-button v-for="slot in groupedSlots[date]" :key="slot" :value="slot">
                {{ formatTime(slot) }}
              </a-radio-button>
            </a-radio-group>
          </a-tab-pane>
        </a-tabs>

        <a-divider />
        <a-button type="primary" :disabled="!selectedSlot" :loading="submitting" @click="handleBook">
          确认预约
        </a-button>
      </div>

      <!-- Success -->
      <a-result v-if="booked" status="success" title="预约成功！" sub-title="请准时参加面试，HR将稍后与您联系。">
        <template #extra>
          <a-button type="primary" @click="booked = false">返回</a-button>
        </template>
      </a-result>
    </a-card>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { message } from 'ant-design-vue'
import axios from 'axios'

const route = useRoute()
const sessionId = computed(() => route.params.sessionToken || route.params.sessionId || 1)

const loading = ref(true)
const error = ref('')
const booked = ref(false)
const submitting = ref(false)
const slots = ref([])
const selectedSlot = ref(null)
const selectedDate = ref('')
const sessionInfo = ref({})

onMounted(async () => {
  try {
    const id = sessionId.value
    const [slotsRes, statusRes] = await Promise.all([
      axios.get(`/api/ai-interview-sessions/${id}/slots`),
      axios.get(`/api/ai-interview-sessions/${id}/booking-status`)
    ])
    slots.value = slotsRes.data.data?.slots || []
    sessionInfo.value = statusRes.data.data || {}
    if (slots.value.length > 0) {
      selectedDate.value = slots.value[0].split('T')[0]
    }
  } catch (e) {
    error.value = '加载可用时段失败，请稍后重试'
  } finally {
    loading.value = false
  }
})

const groupedSlots = computed(() => {
  const groups = {}
  for (const slot of slots.value) {
    const date = slot.split('T')[0]
    if (!groups[date]) groups[date] = []
    groups[date].push(slot)
  }
  return groups
})

function formatDateLabel(dateStr) {
  const d = new Date(dateStr)
  return `${d.getMonth()+1}月${d.getDate()}日`
}

function formatTime(isoStr) {
  const d = new Date(isoStr)
  return `${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`
}

async function handleBook() {
  if (!selectedSlot.value) return
  submitting.value = true
  try {
    await axios.post(`/api/ai-interview-sessions/${sessionId.value}/book`, {
      slotTime: selectedSlot.value
    })
    booked.value = true
  } catch (e) {
    message.error('预约失败，请稍后重试')
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.booking-container {
  max-width: 600px;
  margin: 40px auto;
  padding: 0 16px;
}
.booking-card {
  border-radius: 8px;
}
.slot-group {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.slot-group :deep(.ant-radio-button-wrapper) {
  min-width: 80px;
  text-align: center;
}
</style>