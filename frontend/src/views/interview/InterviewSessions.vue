<template>
  <div class="interview-sessions">
    <div class="toolbar">
      <a-space>
        <a-select
          v-model:value="filterStatus"
          placeholder="筛选状态"
          style="width: 150px"
          allow-clear
          @change="handleSearch"
        >
          <a-select-option value="Pending">待开始</a-select-option>
          <a-select-option value="InProgress">进行中</a-select-option>
          <a-select-option value="Completed">已完成</a-select-option>
          <a-select-option value="Cancelled">已取消</a-select-option>
          <a-select-option value="NoShow">缺席</a-select-option>
        </a-select>
        <a-range-picker
          v-model:value="dateRange"
          @change="handleSearch"
          style="width: 250px"
        />
        <a-button type="primary" @click="handleSearch">
          <template #icon><SearchOutlined /></template>
          搜索
        </a-button>
      </a-space>
      <a-button type="primary" @click="showCreateModal = true">
        <template #icon><PlusOutlined /></template>
        创建会话
      </a-button>
    </div>

    <a-table
      :columns="columns"
      :data-source="sessions"
      :loading="loading"
      :pagination="pagination"
      @change="handleTableChange"
      row-key="id"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'status'">
          <a-tag :color="statusMap[record.status]?.color">
            {{ statusMap[record.status]?.text || record.status }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'duration'">
          {{ formatDuration(record.durationSeconds) }}
        </template>
        <template v-else-if="column.key === 'startTime'">
          {{ record.startTime ? formatDateTime(record.startTime) : '-' }}
        </template>
        <template v-else-if="column.key === 'actions'">
          <a-space>
            <a-button
              v-if="record.status === 'Pending'"
              type="link"
              size="small"
              @click="handleStart(record)"
            >
              开始
            </a-button>
            <a-button type="link" size="small" @click="handleView(record)">
              查看详情
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <!-- Create Modal -->
    <a-modal
      v-model:open="showCreateModal"
      title="创建AI面试会话"
      @ok="handleCreate"
      :confirmLoading="createLoading"
      width="500px"
    >
      <a-form :model="createForm" layout="vertical">
        <a-form-item label="面试邀约" required>
          <a-select
            v-model:value="createForm.interviewInvitationId"
            placeholder="请选择面试邀约"
            show-search
            :filter-option="filterInvitationOption"
          >
            <a-select-option
              v-for="inv in invitations"
              :key="inv.id"
              :value="inv.id"
            >
              {{ inv.candidateName }} - {{ inv.jobPostTitle }}
            </a-select-option>
          </a-select>
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- Detail Modal -->
    <a-modal
      v-model:open="showDetailModal"
      title="会话详情"
      :footer="null"
      width="600px"
    >
      <a-descriptions :column="1" bordered>
        <a-descriptions-item label="ID">{{ currentRecord?.id }}</a-descriptions-item>
        <a-descriptions-item label="候选人">{{ currentRecord?.candidateName }}</a-descriptions-item>
        <a-descriptions-item label="应聘职位">{{ currentRecord?.jobPostTitle }}</a-descriptions-item>
        <a-descriptions-item label="状态">
          <a-tag :color="statusMap[currentRecord?.status]?.color">
            {{ statusMap[currentRecord?.status]?.text || currentRecord?.status }}
          </a-tag>
        </a-descriptions-item>
        <a-descriptions-item label="开始时间">
          {{ currentRecord?.startTime ? formatDateTime(currentRecord.startTime) : '-' }}
        </a-descriptions-item>
        <a-descriptions-item label="结束时间">
          {{ currentRecord?.endTime ? formatDateTime(currentRecord.endTime) : '-' }}
        </a-descriptions-item>
        <a-descriptions-item label="时长">
          {{ currentRecord ? formatDuration(currentRecord.durationSeconds) : '-' }}
        </a-descriptions-item>
        <a-descriptions-item label="会话Token">
          <a :href="`/interview/candidate?token=${currentRecord?.sessionToken}`" target="_blank">
            {{ currentRecord?.sessionToken }}
          </a>
        </a-descriptions-item>
        <a-descriptions-item label="AI评分">{{ currentRecord?.overallScore || '-' }}</a-descriptions-item>
        <a-descriptions-item label="AI评语">{{ currentRecord?.aiComments || '-' }}</a-descriptions-item>
        <a-descriptions-item label="创建时间">
          {{ currentRecord ? formatDateTime(currentRecord.createdAt) : '' }}
        </a-descriptions-item>
      </a-descriptions>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { SearchOutlined, PlusOutlined } from '@ant-design/icons-vue'
import { aiInterviewSessionApi } from '@/api/aiInterviewSession'
import { interviewApi } from '@/api/interview'
import dayjs from 'dayjs'

const statusMap = {
  'Pending': { color: 'default', text: '待开始' },
  'InProgress': { color: 'processing', text: '进行中' },
  'Completed': { color: 'success', text: '已完成' },
  'Cancelled': { color: 'error', text: '已取消' },
  'NoShow': { color: 'warning', text: '缺席' }
}

const sessions = ref([])
const invitations = ref([])
const loading = ref(false)
const createLoading = ref(false)
const showCreateModal = ref(false)
const showDetailModal = ref(false)
const currentRecord = ref(null)
const filterStatus = ref(null)
const dateRange = ref(null)

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
  showSizeChanger: true,
  showTotal: (total) => `共 ${total} 条`
})

const createForm = ref({
  interviewInvitationId: null
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '候选人', dataIndex: 'candidateName', key: 'candidateName' },
  { title: '应聘职位', dataIndex: 'jobPostTitle', key: 'jobPostTitle' },
  { title: '状态', key: 'status', width: 100 },
  { title: '开始时间', key: 'startTime', width: 160 },
  { title: '时长', key: 'duration', width: 100 },
  { title: '操作', key: 'actions', width: 150, fixed: 'right' }
]

function formatDateTime(date) {
  return dayjs(date).format('YYYY-MM-DD HH:mm')
}

function formatDuration(seconds) {
  if (!seconds) return '-'
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}分${secs}秒`
}

function filterInvitationOption(input, option) {
  return option.children[0].children.toLowerCase().includes(input.toLowerCase())
}

async function fetchSessions() {
  loading.value = true
  try {
    const params = {
      page: pagination.current,
      pageSize: pagination.pageSize,
      status: filterStatus.value || undefined,
      dateFrom: dateRange.value?.[0]?.toISOString(),
      dateTo: dateRange.value?.[1]?.toISOString()
    }
    const res = await aiInterviewSessionApi.list(params)
    sessions.value = res.data?.items || res.items || []
    pagination.total = res.data?.total || res.total || 0
  } catch (e) {
    message.error('获取会话列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchInvitations() {
  try {
    const res = await interviewApi.list({ pageSize: 100 })
    invitations.value = res.data?.items || res.items || []
  } catch (e) {
    console.error(e)
  }
}

function handleSearch() {
  pagination.current = 1
  fetchSessions()
}

function handleTableChange(pag) {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  fetchSessions()
}

function handleView(record) {
  currentRecord.value = record
  showDetailModal.value = true
}

async function handleStart(record) {
  try {
    await aiInterviewSessionApi.start(record.id)
    message.success('面试已开始')
    fetchSessions()
  } catch (e) {
    message.error('启动面试失败')
  }
}

async function handleCreate() {
  if (!createForm.value.interviewInvitationId) {
    message.error('请选择面试邀约')
    return
  }

  createLoading.value = true
  try {
    await aiInterviewSessionApi.create({ interviewInvitationId: createForm.value.interviewInvitationId })
    message.success('创建成功')
    showCreateModal.value = false
    createForm.value = { interviewInvitationId: null }
    fetchSessions()
  } catch (e) {
    message.error('创建失败')
  } finally {
    createLoading.value = false
  }
}

onMounted(() => {
  fetchSessions()
  fetchInvitations()
})
</script>

<style scoped>
.interview-sessions {
  height: 100%;
}

.toolbar {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}
</style>
