<template>
  <div class="match-result-list">
    <div class="header-actions">
      <a-space>
        <a-select
          v-model:value="jobPostId"
          placeholder="选择职位"
          style="width: 200px"
          allowClear
          show-search
          :filter-option="false"
          @search="handleJobPostSearch"
          @change="handleSearch"
        >
          <a-select-option v-for="job in jobPosts" :key="job.id" :value="job.id">
            {{ job.title }}
          </a-select-option>
        </a-select>
      </a-space>
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
        <template v-if="column.key === 'score'">
          <a-progress
            :percent="record.score"
            :status="getScoreStatus(record.score)"
            size="small"
          />
          <span style="margin-left: 8px">{{ record.score }}分</span>
        </template>
        <template v-else-if="column.key === 'matchedSkills'">
          <a-tag v-for="skill in parseSkills(record.matchedSkills)" :key="skill" color="success">
            {{ skill }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'missingSkills'">
          <a-tag v-for="skill in parseSkills(record.missingSkills)" :key="skill" color="warning">
            {{ skill }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'status'">
          <a-tag :color="getStatusColor(record.status)">
            {{ getStatusText(record.status) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'action'">
          <a-dropdown>
            <a>操作</a>
            <template #overlay>
              <a-menu>
                <a-menu-item @click="handleViewDetail(record)">查看详情</a-menu-item>
                <a-menu-divider />
                <a-menu-item @click="handleUpdateStatus(record.id, 'Accepted')">录用</a-menu-item>
                <a-menu-item @click="handleUpdateStatus(record.id, 'Rejected')">不合适</a-menu-item>
              </a-menu>
            </template>
          </a-dropdown>
        </template>
      </template>
    </a-table>

    <a-modal
      v-model:open="detailVisible"
      title="匹配详情"
      :footer="null"
      width="600px"
    >
      <a-descriptions v-if="currentRecord" :column="1" bordered>
        <a-descriptions-item label="简历ID">{{ currentRecord.resumeId }}</a-descriptions-item>
        <a-descriptions-item label="匹配分数">
          <a-progress :percent="currentRecord.score" :status="getScoreStatus(currentRecord.score)" />
        </a-descriptions-item>
        <a-descriptions-item label="匹配技能">
          <a-tag v-for="skill in parseSkills(currentRecord.matchedSkills)" :key="skill" color="success">
            {{ skill }}
          </a-tag>
          <span v-if="!parseSkills(currentRecord.matchedSkills).length">-</span>
        </a-descriptions-item>
        <a-descriptions-item label="缺失技能">
          <a-tag v-for="skill in parseSkills(currentRecord.missingSkills)" :key="skill" color="warning">
            {{ skill }}
          </a-tag>
          <span v-if="!parseSkills(currentRecord.missingSkills).length">-</span>
        </a-descriptions-item>
        <a-descriptions-item label="匹配理由">{{ currentRecord.summary }}</a-descriptions-item>
      </a-descriptions>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { matchApi } from '@/api/match'
import { jobPostApi } from '@/api/jobpost'
import { message } from 'ant-design-vue'

const route = useRoute()
const loading = ref(false)
const jobPostId = ref(route.query.jobPostId ? parseInt(route.query.jobPostId) : null)
const dataSource = ref([])
const jobPosts = ref([])
const pagination = ref({
  current: 1,
  pageSize: 20,
  total: 0
})

const detailVisible = ref(false)
const currentRecord = ref(null)

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '简历ID', dataIndex: 'resumeId', key: 'resumeId', width: 80 },
  { title: '职位ID', dataIndex: 'jobPostId', key: 'jobPostId', width: 80 },
  { title: '匹配分数', key: 'score', width: 180 },
  { title: '匹配技能', key: 'matchedSkills', width: 200 },
  { title: '缺失技能', key: 'missingSkills', width: 200 },
  { title: '状态', key: 'status', width: 100 },
  { title: '操作', key: 'action', width: 100, fixed: 'right' }
]

function parseSkills(skillsJson) {
  if (!skillsJson) return []
  try {
    return JSON.parse(skillsJson)
  } catch {
    return []
  }
}

function getScoreStatus(score) {
  if (score >= 80) return 'success'
  if (score >= 60) return 'active'
  return 'exception'
}

function getStatusColor(status) {
  const colors = {
    Pending: 'default',
    Reviewed: 'processing',
    Accepted: 'success',
    Rejected: 'error'
  }
  return colors[status] || 'default'
}

function getStatusText(status) {
  const texts = {
    Pending: '待查看',
    Reviewed: '已查看',
    Accepted: '录用',
    Rejected: '不合适'
  }
  return texts[status] || status
}

async function fetchData() {
  loading.value = true
  try {
    const res = await matchApi.list({
      jobPostId: jobPostId.value,
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

async function fetchJobPosts() {
  try {
    const res = await jobPostApi.list({ pageSize: 100 })
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

async function handleJobPostSearch(value) {
  if (value) {
    try {
      const res = await jobPostApi.list({ keyword: value, pageSize: 20 })
      jobPosts.value = res.data?.items || []
    } catch (e) {
      console.error(e)
    }
  }
}

function handleViewDetail(record) {
  currentRecord.value = record
  detailVisible.value = true
}

async function handleUpdateStatus(id, status) {
  try {
    await matchApi.updateStatus(id, status)
    message.success('状态更新成功')
    fetchData()
  } catch (e) {
    console.error(e)
  }
}

onMounted(() => {
  fetchJobPosts()
  fetchData()
})
</script>

<style scoped>
.match-result-list {
  padding: 24px;
}

.header-actions {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}
</style>
