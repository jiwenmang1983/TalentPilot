<template>
  <div class="candidate-confirm-container">
    <div class="confirm-card">
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

      <div v-else-if="invitation" class="invitation-content">
        <div class="company-logo">
          <a-avatar :size="80" style="background-color: #1890ff">
            <template #icon><ScheduleOutlined /></template>
          </a-avatar>
        </div>

        <h1 class="title">面试邀请</h1>
        <p class="company">{{ invitation.companyName }}</p>

        <a-divider />

        <a-descriptions :column="1" size="small">
          <a-descriptions-item label="尊敬的候选人">
            <strong>{{ invitation.candidateName }}</strong>
          </a-descriptions-item>
          <a-descriptions-item label="应聘职位">
            <strong>{{ invitation.jobPostTitle }}</strong>
          </a-descriptions-item>
          <a-descriptions-item label="可选时间窗口">
            {{ formatTimeSlot(invitation.timeSlotStart, invitation.timeSlotEnd) }}
          </a-descriptions-item>
          <a-descriptions-item v-if="invitation.interviewTime" label="HR建议时间">
            {{ formatDate(invitation.interviewTime) }}
          </a-descriptions-item>
        </a-descriptions>

        <a-divider />

        <div v-if="invitation.status === 'PendingConfirmation'" class="action-section">
          <p class="action-hint">请选择您的面试时间并确认参加，或拒绝此次邀请</p>

          <a-form :model="confirmForm" layout="vertical" class="confirm-form">
            <a-form-item label="选择面试时间" required>
              <a-date-picker
                v-model:value="confirmForm.interviewTime"
                show-time
                format="YYYY-MM-DD HH:mm"
                :disabled-date="disabledDate"
                style="width: 100%"
              />
            </a-form-item>
          </a-form>

          <div class="action-buttons">
            <a-button type="primary" size="large" :loading="confirmLoading" @click="handleConfirm">
              确认参加
            </a-button>
            <a-button size="large" :loading="refuseLoading" @click="handleRefuse">
              拒绝邀请
            </a-button>
          </div>
        </div>

        <div v-else-if="invitation.status === 'Confirmed'" class="success-section">
          <a-result status="success" title="已确认参加">
            <template #subTitle>
              <p>您的面试时间：{{ invitation.interviewTime ? formatDate(invitation.interviewTime) : '待定' }}</p>
              <p class="tips">我们已将您的确认信息通知招聘方，请保持手机畅通</p>
            </template>
          </a-result>
        </div>

        <div v-else-if="invitation.status === 'Refused'" class="refused-section">
          <a-result status="info" title="已拒绝">
            <template #subTitle>
              <p>感谢您的回复，招聘方已收到您的拒绝通知</p>
            </template>
          </a-result>
        </div>

        <div v-else-if="invitation.status === 'Cancelled'" class="cancelled-section">
          <a-result status="warning" title="邀请已取消">
            <template #subTitle>
              <p>此面试邀请已被招聘方取消</p>
            </template>
          </a-result>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ScheduleOutlined } from '@ant-design/icons-vue'
import { interviewApi } from '@/api/interview'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'

const route = useRoute()
const loading = ref(true)
const error = ref(null)
const invitation = ref(null)
const confirmLoading = ref(false)
const refuseLoading = ref(false)

const confirmForm = ref({
  interviewTime: null
})

function disabledDate(current) {
  if (!invitation.value) return true
  const start = dayjs(invitation.value.timeSlotStart)
  const end = dayjs(invitation.value.timeSlotEnd)
  return current < start.startOf('day') || current > end.endOf('day')
}

function formatDate(date) {
  return dayjs(date).format('YYYY-MM-DD HH:mm')
}

function formatTimeSlot(start, end) {
  return `${formatDate(start)} - ${dayjs(end).format('HH:mm')}`
}

async function fetchInvitation() {
  const token = route.params.token
  if (!token) {
    error.value = '无效的邀请链接'
    loading.value = false
    return
  }

  try {
    const res = await interviewApi.getByToken(token)
    if (res.success && res.data) {
      invitation.value = res.data
      if (res.data.interviewTime) {
        confirmForm.value.interviewTime = dayjs(res.data.interviewTime)
      }
    } else {
      error.value = res.message || '邀请不存在或链接已失效'
    }
  } catch (e) {
    console.error(e)
    error.value = '加载失败，请检查链接是否正确'
  } finally {
    loading.value = false
  }
}

async function handleConfirm() {
  if (!confirmForm.value.interviewTime) {
    message.error('请选择面试时间')
    return
  }

  confirmLoading.value = true
  try {
    const res = await interviewApi.confirm(invitation.value.id, {
      interviewTime: confirmForm.value.interviewTime.toISOString()
    })
    if (res.success) {
      invitation.value.status = 'Confirmed'
      invitation.value.interviewTime = confirmForm.value.interviewTime.toISOString()
      message.success('确认成功')
    } else {
      message.error(res.message || '确认失败')
    }
  } catch (e) {
    console.error(e)
    message.error('确认失败')
  } finally {
    confirmLoading.value = false
  }
}

async function handleRefuse() {
  refuseLoading.value = true
  try {
    const res = await interviewApi.refuse(invitation.value.id)
    if (res.success) {
      invitation.value.status = 'Refused'
      message.success('已拒绝')
    } else {
      message.error(res.message || '操作失败')
    }
  } catch (e) {
    console.error(e)
    message.error('操作失败')
  } finally {
    refuseLoading.value = false
  }
}

onMounted(() => {
  fetchInvitation()
})
</script>

<style scoped>
.candidate-confirm-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
}

.confirm-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  padding: 40px;
  width: 100%;
  max-width: 500px;
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

.invitation-content {
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

.action-section {
  padding: 20px 0;
}

.action-hint {
  color: #666;
  margin-bottom: 20px;
}

.confirm-form {
  text-align: left;
  margin-bottom: 24px;
}

.action-buttons {
  display: flex;
  gap: 16px;
  justify-content: center;
}

.action-buttons button {
  min-width: 120px;
}

.success-section,
.refused-section,
.cancelled-section {
  padding: 20px 0;
}

.tips {
  color: #888;
  font-size: 14px;
  margin-top: 8px;
}
</style>
