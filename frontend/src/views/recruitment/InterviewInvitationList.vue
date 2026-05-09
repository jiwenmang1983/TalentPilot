<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>📨 面试邀请</h1>
      <p>批量发送面试邀请</p>
    </div>
  </div>

  <div class="interview-invitation-list">
    <div class="header-actions">
      <a-select
        v-model:value="filterStatus"
        placeholder="筛选状态"
        style="width: 180px"
        allowClear
        @change="handleSearch"
      >
        <a-select-option value="PendingConfirmation">待确认</a-select-option>
        <a-select-option value="Confirmed">已确认</a-select-option>
        <a-select-option value="Refused">已拒绝</a-select-option>
        <a-select-option value="Cancelled">已取消</a-select-option>
      </a-select>
      <a-button type="primary" @click="showCreateModal = true">
        <template #icon><PlusOutlined /></template>
        创建邀请
      </a-button>
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
        <template v-if="column.key === 'status'">
          <a-tag :color="getStatusColor(record.status)">
            {{ getStatusText(record.status) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'timeSlot'">
          {{ formatTimeSlot(record.timeSlotStart, record.timeSlotEnd) }}
        </template>
        <template v-else-if="column.key === 'interviewTime'">
          {{ record.interviewTime ? formatDate(record.interviewTime) : '-' }}
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a @click="handleView(record)">查看</a>
            <a-divider type="vertical" />
            <a-dropdown>
              <a>更多</a>
              <template #overlay>
                <a-menu>
                  <a-menu-item v-if="record.status === 'PendingConfirmation'" @click="handleSend(record)">
                    发送邀请
                  </a-menu-item>
                  <a-menu-item v-if="record.status === 'PendingConfirmation'" @click="handleEdit(record)">
                    编辑
                  </a-menu-item>
                  <a-menu-item v-if="record.status === 'PendingConfirmation'" @click="handleCancel(record)">
                    取消邀请
                  </a-menu-item>
                  <a-menu-item v-if="record.status === 'Confirmed'" @click="handleCancel(record)">
                    取消面试
                  </a-menu-item>
                  <a-menu-divider />
                  <a-menu-item v-if="record.status === 'PendingConfirmation'" @click="handleDelete(record.id)" style="color: #ff4d4f">
                    删除
                  </a-menu-item>
                </a-menu>
              </template>
            </a-dropdown>
          </a-space>
        </template>
      </template>
    </a-table>

    <!-- Create Modal -->
    <a-modal
      v-model:open="showCreateModal"
      title="创建面试邀请"
      @ok="handleCreate"
      :confirmLoading="createLoading"
      width="600px"
    >
      <a-form :model="createForm" :label-col="{ span: 6 }" layout="horizontal">
        <a-form-item label="候选人" required>
          <a-select
            v-model:value="createForm.candidateId"
            placeholder="请选择候选人"
            show-search
            :filter-option="filterCandidateOption"
          >
            <a-select-option v-for="c in candidates" :key="c.id" :value="c.id">
              {{ c.name }} - {{ c.email }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="职位" required>
          <a-select
            v-model:value="createForm.jobPostId"
            placeholder="请选择职位"
            show-search
            :filter-option="filterJobPostOption"
          >
            <a-select-option v-for="j in jobPosts" :key="j.id" :value="j.id">
              {{ j.title }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="时间窗口" required>
          <a-range-picker
            v-model:value="createForm.timeSlot"
            show-time
            format="YYYY-MM-DD HH:mm"
          />
        </a-form-item>
        <a-form-item label="预设面试时间">
          <a-date-picker
            v-model:value="createForm.interviewTime"
            show-time
            format="YYYY-MM-DD HH:mm"
            placeholder="可选，候选人可修改"
            style="width: 100%"
          />
        </a-form-item>
        <a-form-item label="备注">
          <a-textarea v-model:value="createForm.notes" placeholder="HR备注信息" :rows="3" />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- Edit Modal -->
    <a-modal
      v-model:open="showEditModal"
      title="编辑面试邀请"
      @ok="handleUpdate"
      :confirmLoading="updateLoading"
      width="600px"
    >
      <a-form :model="editForm" :label-col="{ span: 6 }" layout="horizontal">
        <a-form-item label="预设面试时间">
          <a-date-picker
            v-model:value="editForm.interviewTime"
            show-time
            format="YYYY-MM-DD HH:mm"
            style="width: 100%"
          />
        </a-form-item>
        <a-form-item label="备注">
          <a-textarea v-model:value="editForm.notes" placeholder="HR备注信息" :rows="3" />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- Detail Modal -->
    <a-modal
      v-model:open="showDetailModal"
      title="邀请详情"
      :footer="null"
      width="600px"
    >
      <a-descriptions :column="1" bordered>
        <a-descriptions-item label="候选人">{{ currentRecord?.candidateName }}</a-descriptions-item>
        <a-descriptions-item label="联系方式">{{ currentRecord?.candidatePhone || '-' }} / {{ currentRecord?.candidateEmail || '-' }}</a-descriptions-item>
        <a-descriptions-item label="应聘职位">{{ currentRecord?.jobPostTitle }}</a-descriptions-item>
        <a-descriptions-item label="邀请状态">
          <a-tag :color="getStatusColor(currentRecord?.status)">{{ getStatusText(currentRecord?.status) }}</a-tag>
        </a-descriptions-item>
        <a-descriptions-item label="可选时间窗口">{{ currentRecord ? formatTimeSlot(currentRecord.timeSlotStart, currentRecord.timeSlotEnd) : '' }}</a-descriptions-item>
        <a-descriptions-item label="确认面试时间">{{ currentRecord?.interviewTime ? formatDate(currentRecord.interviewTime) : '-' }}</a-descriptions-item>
        <a-descriptions-item label="邀请链接">
          <a :href="`/interview/confirm/${currentRecord?.inviteToken}`" target="_blank">
            {{ currentRecord?.inviteToken }}
          </a>
        </a-descriptions-item>
        <a-descriptions-item label="备注">{{ currentRecord?.notes || '-' }}</a-descriptions-item>
        <a-descriptions-item label="创建时间">{{ currentRecord ? formatDate(currentRecord.createdAt) : '' }}</a-descriptions-item>
        <a-descriptions-item label="确认时间">{{ currentRecord?.confirmedAt ? formatDate(currentRecord.confirmedAt) : '-' }}</a-descriptions-item>
        <a-descriptions-item label="拒绝时间">{{ currentRecord?.refusedAt ? formatDate(currentRecord.refusedAt) : '-' }}</a-descriptions-item>
      </a-descriptions>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { PlusOutlined } from '@ant-design/icons-vue'
import { interviewApi } from '@/api/interview'
import { candidateApi } from '@/api/candidate'
import { jobPostApi } from '@/api/jobpost'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'

const loading = ref(false)
const createLoading = ref(false)
const updateLoading = ref(false)
const filterStatus = ref(null)
const dataSource = ref([])
const pagination = ref({
  current: 1,
  pageSize: 20,
  total: 0
})

const showCreateModal = ref(false)
const showEditModal = ref(false)
const showDetailModal = ref(false)
const currentRecord = ref(null)

const candidates = ref([])
const jobPosts = ref([])

const createForm = ref({
  candidateId: null,
  jobPostId: null,
  timeSlot: null,
  interviewTime: null,
  notes: ''
})

const editForm = ref({
  interviewTime: null,
  notes: ''
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '候选人', dataIndex: 'candidateName', key: 'candidateName' },
  { title: '联系方式', key: 'contact', width: 180 },
  { title: '应聘职位', dataIndex: 'jobPostTitle', key: 'jobPostTitle' },
  { title: '状态', key: 'status', width: 100 },
  { title: '可选时间', key: 'timeSlot', width: 200 },
  { title: '确认时间', key: 'interviewTime', width: 160 },
  { title: '操作', key: 'action', width: 150, fixed: 'right' }
]

function getStatusColor(status) {
  const colors = {
    PendingConfirmation: 'warning',
    Confirmed: 'success',
    Refused: 'error',
    Cancelled: 'default'
  }
  return colors[status] || 'default'
}

function getStatusText(status) {
  const texts = {
    PendingConfirmation: '待确认',
    Confirmed: '已确认',
    Refused: '已拒绝',
    Cancelled: '已取消'
  }
  return texts[status] || status
}

function formatDate(date) {
  return dayjs(date).format('YYYY-MM-DD HH:mm')
}

function formatTimeSlot(start, end) {
  return `${formatDate(start)} - ${dayjs(end).format('HH:mm')}`
}

function filterCandidateOption(input, option) {
  return option.children[0].children.toLowerCase().includes(input.toLowerCase())
}

function filterJobPostOption(input, option) {
  return option.children[0].children.toLowerCase().includes(input.toLowerCase())
}

async function fetchData() {
  loading.value = true
  try {
    const res = await interviewApi.list({
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

async function fetchJobPosts() {
  try {
    const res = await jobPostApi.list({ status: 'Published', pageSize: 100 })
    jobPosts.value = res.data?.items || []
  } catch (e) {
    console.error(e)
  }
}

function handleSearch() {
  pagination.value.current = 1
  fetchData()
}

function handleTableChange(pag) {
  pagination.value.current = pag.current
  pagination.value.pageSize = pag.pageSize
  fetchData()
}

function handleView(record) {
  currentRecord.value = record
  showDetailModal.value = true
}

function handleEdit(record) {
  currentRecord.value = record
  editForm.value = {
    interviewTime: record.interviewTime ? dayjs(record.interviewTime) : null,
    notes: record.notes || ''
  }
  showEditModal.value = true
}

async function handleCreate() {
  if (!createForm.value.candidateId || !createForm.value.jobPostId || !createForm.value.timeSlot) {
    message.error('请填写必填项')
    return
  }

  createLoading.value = true
  try {
    await interviewApi.create({
      candidateId: createForm.value.candidateId,
      jobPostId: createForm.value.jobPostId,
      timeSlotStart: createForm.value.timeSlot[0].toISOString(),
      timeSlotEnd: createForm.value.timeSlot[1].toISOString(),
      interviewTime: createForm.value.interviewTime?.toISOString(),
      notes: createForm.value.notes
    })
    message.success('创建成功')
    showCreateModal.value = false
    createForm.value = {
      candidateId: null,
      jobPostId: null,
      timeSlot: null,
      interviewTime: null,
      notes: ''
    }
    fetchData()
  } catch (e) {
    console.error(e)
  } finally {
    createLoading.value = false
  }
}

async function handleUpdate() {
  updateLoading.value = true
  try {
    await interviewApi.update(currentRecord.value.id, {
      interviewTime: editForm.value.interviewTime?.toISOString(),
      notes: editForm.value.notes
    })
    message.success('更新成功')
    showEditModal.value = false
    fetchData()
  } catch (e) {
    console.error(e)
  } finally {
    updateLoading.value = false
  }
}

async function handleSend(record) {
  try {
    await interviewApi.send(record.id)
    message.success('发送成功')
    fetchData()
  } catch (e) {
    console.error(e)
  }
}

async function handleCancel(record) {
  try {
    await interviewApi.cancel(record.id)
    message.success('已取消')
    fetchData()
  } catch (e) {
    console.error(e)
  }
}

async function handleDelete(id) {
  try {
    await interviewApi.delete(id)
    message.success('删除成功')
    fetchData()
  } catch (e) {
    console.error(e)
  }
}

onMounted(() => {
  fetchData()
  fetchCandidates()
  fetchJobPosts()
})
</script>

<style scoped>
.interview-invitation-list {
  padding: 24px;
}

.header-actions {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}
</style>
