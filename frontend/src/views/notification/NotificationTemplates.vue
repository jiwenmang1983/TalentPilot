<template>
  <div class="notification-templates">
    <div class="page-header">
      <div class="page-header-left">
        <h1>📝 通知模板</h1>
        <p>管理通知邮件模板</p>
      </div>
    </div>

    <a-table
      :columns="columns"
      :dataSource="templates"
      :loading="loading"
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
        <template v-else-if="column.key === 'subject'">
          <span class="subject-cell">{{ record.subject }}</span>
        </template>
        <template v-else-if="column.key === 'action'">
          <a @click="handleEdit(record)">编辑</a>
        </template>
      </template>
    </a-table>

    <!-- Edit Template Modal -->
    <a-modal
      v-model:open="showEditModal"
      title="编辑模板"
      @ok="handleUpdate"
      :confirmLoading="updateLoading"
      width="640px"
    >
      <a-form :model="editForm" :label-col="{ span: 5 }" layout="horizontal">
        <a-form-item label="模板名称">
          <a-input v-model:value="editForm.name" disabled />
        </a-form-item>
        <a-form-item label="通知类型">
          <a-input v-model:value="editForm.notificationType" disabled />
        </a-form-item>
        <a-form-item label="发送渠道">
          <a-input v-model:value="editForm.channel" disabled />
        </a-form-item>
        <a-form-item label="邮件主题">
          <a-input v-model:value="editForm.subject" />
        </a-form-item>
        <a-form-item label="邮件内容">
          <a-textarea v-model:value="editForm.content" :rows="8" />
        </a-form-item>
        <a-divider>可用变量</a-divider>
        <div class="template-vars">
          <a-tag>{"{{candidate_name}}"} - 候选人姓名</a-tag>
          <a-tag>{"{{job_title}}"} - 职位名称</a-tag>
          <a-tag>{"{{interview_time}}"} - 面试时间</a-tag>
          <a-tag>{"{{interview_mode}}"} - 面试模式</a-tag>
          <a-tag>{"{{company_name}}"} - 公司名称</a-tag>
        </div>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { notificationApi } from '@/api/notification'
import { message } from 'ant-design-vue'

const loading = ref(false)
const updateLoading = ref(false)
const templates = ref([])
const showEditModal = ref(false)

const editForm = ref({
  id: null,
  name: '',
  notificationType: '',
  channel: '',
  subject: '',
  content: ''
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '模板名称', dataIndex: 'name', key: 'name', width: 150 },
  { title: '类型', key: 'notificationType', width: 120 },
  { title: '渠道', key: 'channel', width: 100 },
  { title: '邮件主题', key: 'subject', ellipsis: true },
  { title: '操作', key: 'action', width: 80 }
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

async function fetchTemplates() {
  loading.value = true
  try {
    const res = await notificationApi.getTemplates()
    templates.value = res.data || []
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

function handleEdit(record) {
  editForm.value = { ...record }
  showEditModal.value = true
}

async function handleUpdate() {
  updateLoading.value = true
  try {
    await notificationApi.updateTemplate(editForm.value.id, {
      subject: editForm.value.subject,
      content: editForm.value.content
    })
    message.success('更新成功')
    showEditModal.value = false
    fetchTemplates()
  } catch (e) {
    console.error(e)
  } finally {
    updateLoading.value = false
  }
}

onMounted(() => {
  fetchTemplates()
})
</script>

<style scoped>
.notification-templates {
  padding: 24px;
}

.page-header {
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

.subject-cell {
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  display: block;
}

.template-vars {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
</style>