<template>
  <div class="notification-list">
    <div class="page-header">
      <div class="page-header-left">
        <h1>📨 通知日志</h1>
        <p>查看发送记录和状态</p>
      </div>
      <div class="page-header-right">
        <a-button type="primary" @click="showSendModal = true">
          <template #icon><PlusOutlined /></template>
          发送通知
        </a-button>
      </div>
    </div>

    <div class="filter-bar">
      <a-select
        v-model:value="filterType"
        placeholder="通知类型"
        style="width: 160px"
        allowClear
        @change="handleSearch"
      >
        <a-select-option value="InterviewInvitation">面试邀请</a-select-option>
        <a-select-option value="InterviewReminder">面试提醒</a-select-option>
        <a-select-option value="Offer">Offer</a-select-option>
      </a-select>
      <a-select
        v-model:value="filterStatus"
        placeholder="发送状态"
        style="width: 140px"
        allowClear
        @change="handleSearch"
      >
        <a-select-option value="Pending">待发送</a-select-option>
        <a-select-option value="Sent">已发送</a-select-option>
        <a-select-option value="Failed">发送失败</a-select-option>
      </a-select>
      <a-button @click="handleReset">重置</a-button>
    </div>

    <a-table
      :columns="columns"
      :dataSource="dataSource"
      :loading="loading"
      :pagination="pagination"
      @change="handleTableChange"
      rowKey="id"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'notificationType'">
          <a-tag :color="getTypeColor(record.notificationType)">
            {{ getTypeText(record.notificationType) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'channel'">
          <span>{{ record.channel === 'Email' ? '📧 邮件' : '📱 短信' }}</span>
        </template>
        <template v-else-if="column.key === 'status'">
          <a-tag :color="getStatusColor(record.status)">
            {{ getStatusText(record.status) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'sentAt'">
          {{ record.sentAt ? formatDate(record.sentAt) : '-' }}
        </template>
      </template>
    </a-table>

    <!-- Send Notification Modal -->
    <a-modal
      v-model:open="showSendModal"
      title="发送通知"
      @ok="handleSend"
      :confirmLoading="sendLoading"
      width="560px"
    >
      <a-form :model="sendForm" :label-col="{ span: 6 }" layout="horizontal">
        <a-form-item label="候选人" required>
          <a-select
            v-model:value="sendForm.candidateId"
            placeholder="请选择候选人"
            show-search
            :filter-option="filterCandidateOption"
          >
            <a-select-option v-for="c in candidates" :key="c.id" :value="c.id">
              {{ c.name }} - {{ c.email || c.phone }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="通知类型" required>
          <a-select v-model:value="sendForm.notificationType" placeholder="请选择类型">
            <a-select-option value="InterviewInvitation">面试邀请</a-select-option>
            <a-select-option value="InterviewReminder">面试提醒</a-select-option>
            <a-select-option value="Offer">Offer</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="发送渠道">
          <a-radio-group v-model:value="sendForm.channel">
            <a-radio value="Email">邮件</a-radio>
            <a-radio value="SMS">短信</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-divider>模板变量（可选）</a-divider>
        <a-form-item label="职位名称">
          <a-input v-model:value="sendForm.templateVariables.job_title" placeholder="如：高级前端工程师" />
        </a-form-item>
        <a-form-item label="面试时间">
          <a-date-picker
            v-model:value="sendForm.templateVariables.interview_time"
            show-time
            format="YYYY-MM-DD HH:mm"
            style="width: 100%"
          />
        </a-form-item>
        <a-form-item label="面试模式">
          <a-input v-model:value="sendForm.templateVariables.interview_mode" placeholder="如：视频面试" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { PlusOutlined } from '@ant-design/icons-vue'
import { notificationApi } from '@/api/notification'
import { candidateApi } from '@/api/candidate'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'

const loading = ref(false)
const sendLoading = ref(false)
const filterType = ref(null)
const filterStatus = ref(null)
const dataSource = ref([])
const pagination = ref({
  current: 1,
  pageSize: 20,
  total: 0
})

const showSendModal = ref(false)
const candidates = ref([])

const sendForm = ref({
  candidateId: null,
  notificationType: 'InterviewInvitation',
  channel: 'Email',
  templateVariables: {
    job_title: '',
    interview_time: null,
    interview_mode: ''
  }
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '候选人', dataIndex: 'candidateName', key: 'candidateName' },
  { title: '类型', key: 'notificationType', width: 120 },
  { title: '渠道', key: 'channel', width: 100 },
  { title: '收件人', dataIndex: 'recipient', key: 'recipient' },
  { title: '主题', dataIndex: 'subject', key: 'subject', ellipsis: true },
  { title: '状态', key: 'status', width: 100 },
  { title: '发送时间', key: 'sentAt', width: 160 },
  { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt', width: 160 }
]

function getTypeColor(type) {
  const colors = {
    InterviewInvitation: 'blue',
    InterviewReminder: 'orange',
    Offer: 'green'
  }
  return colors[type] || 'default'
}

function getTypeText(type) {
  const texts = {
    InterviewInvitation: '面试邀请',
    InterviewReminder: '面试提醒',
    Offer: 'Offer'
  }
  return texts[type] || type
}

function getStatusColor(status) {
  const colors = {
    Pending: 'warning',
    Sent: 'success',
    Failed: 'error'
  }
  return colors[status] || 'default'
}

function getStatusText(status) {
  const texts = {
    Pending: '待发送',
    Sent: '已发送',
    Failed: '发送失败'
  }
  return texts[status] || status
}

function formatDate(date) {
  return dayjs(date).format('YYYY-MM-DD HH:mm')
}

function filterCandidateOption(input, option) {
  return option.children[0].children.toLowerCase().includes(input.toLowerCase())
}

async function fetchData() {
  loading.value = true
  try {
    const res = await notificationApi.list({
      notificationType: filterType.value,
      status: filterStatus.value,
      page: pagination.value.current,
      pageSize: pagination.value.pageSize
    })
    dataSource.value = res.data?.items || []
    pagination.value.total = res.data?.total || 0
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function fetchCandidates() {
  try {
    const res = await candidateApi.list({ pageSize: 100 })
    candidates.value = res.data?.items || []
  } catch (e) {
    console.error(e)
  }
}

function handleSearch() {
  pagination.value.current = 1
  fetchData()
}

function handleReset() {
  filterType.value = null
  filterStatus.value = null
  handleSearch()
}

function handleTableChange(pag) {
  pagination.value.current = pag.current
  pagination.value.pageSize = pag.pageSize
  fetchData()
}

async function handleSend() {
  if (!sendForm.value.candidateId) {
    message.error('请选择候选人')
    return
  }

  sendLoading.value = true
  try {
    const payload = {
      candidateId: sendForm.value.candidateId,
      notificationType: sendForm.value.notificationType,
      channel: sendForm.value.channel,
      templateVariables: {}
    }

    if (sendForm.value.templateVariables.job_title) {
      payload.templateVariables.job_title = sendForm.value.templateVariables.job_title
    }
    if (sendForm.value.templateVariables.interview_time) {
      payload.templateVariables.interview_time = sendForm.value.templateVariables.interview_time.toISOString()
    }
    if (sendForm.value.templateVariables.interview_mode) {
      payload.templateVariables.interview_mode = sendForm.value.templateVariables.interview_mode
    }

    await notificationApi.send(payload)
    message.success('发送成功')
    showSendModal.value = false
    sendForm.value = {
      candidateId: null,
      notificationType: 'InterviewInvitation',
      channel: 'Email',
      templateVariables: { job_title: '', interview_time: null, interview_mode: '' }
    }
    fetchData()
  } catch (e) {
    console.error(e)
  } finally {
    sendLoading.value = false
  }
}

onMounted(() => {
  fetchData()
  fetchCandidates()
})
</script>

<style scoped>
.notification-list {
  padding: 24px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.page-header-left h1 {
  margin: 0;
  font-size: 24px;
  font-weight: 600;
}

.page-header-left p {
  margin: 4px 0 0;
  color: #6b7280;
}

.filter-bar {
  display: flex;
  gap: 12px;
  margin-bottom: 16px;
}
</style>