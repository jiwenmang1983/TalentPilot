<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>📄 简历库</h1>
      <p>候选人简历采集与管理 - 按匹配度排序</p>
    </div>
    <div class="page-header-right">
      <a-button type="primary" @click="handleCollectNow" :loading="collecting">
        立即采集
      </a-button>
    </div>
  </div>

  <!-- Source Status Section -->
  <div class="source-status-section">
    <div class="section-title">
      <span>📡 采集渠道状态</span>
      <a-button type="link" size="small" @click="fetchSources">刷新</a-button>
    </div>
    <a-table
      :columns="sourceColumns"
      :dataSource="sourceData"
      :loading="sourcesLoading"
      :pagination="false"
      rowKey="id"
      size="small"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'isActive'">
          <a-tag :color="record.isActive ? 'green' : 'red'">
            {{ record.isActive ? '活跃' : '停用' }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'lastSyncAt'">
          {{ formatDateTime(record.lastSyncAt) }}
        </template>
      </template>
    </a-table>
  </div>

  <div class="resume-list">
    <!-- Filter Bar -->
    <div class="filter-bar">
      <a-space wrap>
        <a-select
          v-model:value="filterJobPostId"
          placeholder="目标职位"
          style="width: 200px"
          allowClear
          @change="handleSearch"
        >
          <a-select-option v-for="job in jobPosts" :key="job.id" :value="job.id">
            {{ job.title }}
          </a-select-option>
        </a-select>
        <a-select
          v-model:value="filterChannel"
          placeholder="采集渠道"
          style="width: 150px"
          allowClear
          @change="handleSearch"
        >
          <a-select-option value="Boss">Boss直聘</a-select-option>
          <a-select-option value="Zhaopin">智联招聘</a-select-option>
          <a-select-option value="Lagou">拉勾网</a-select-option>
          <a-select-option value="Liepin">猎聘网</a-select-option>
          <a-select-option value="ZhiLian">智联招聘</a-select-option>
          <a-select-option value="QianCheng">前程无忧</a-select-option>
          <a-select-option value="Manual">手动录入</a-select-option>
        </a-select>
        <a-input-number
          v-model:value="filterMinScore"
          placeholder="最低分"
          :min="0"
          :max="100"
          style="width: 100px"
          @change="handleSearch"
        />
        <span style="color: #999; line-height: 32px;">-</span>
        <a-input-number
          v-model:value="filterMaxScore"
          placeholder="最高分"
          :min="0"
          :max="100"
          style="width: 100px"
          @change="handleSearch"
        />
        <a-button @click="handleReset">重置</a-button>
      </a-space>
    </div>

    <!-- Table -->
    <a-table
      :columns="columns"
      :dataSource="dataSource"
      :loading="loading"
      :pagination="pagination"
      @change="handleTableChange"
      rowKey="id"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'matchScore'">
          <div v-if="record.matchScore != null" class="score-cell">
            <a-progress
              :percent="Number(record.matchScore)"
              :status="getScoreStatus(record.matchScore, record.matchThreshold)"
              :stroke-color="getScoreColor(record.matchScore, record.matchThreshold)"
              size="small"
              style="width: 120px;"
            />
            <span class="score-text">{{ record.matchScore }}分</span>
          </div>
          <span v-else class="no-match">未匹配</span>
        </template>
        <template v-else-if="column.key === 'source'">
          <a-tag :color="getSourceColor(record.source)">
            {{ getSourceText(record.source) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'matchStatus'">
          <a-tag v-if="record.matchScore != null" :color="getMatchStatusColor(record.matchScore, record.matchThreshold)">
            {{ record.matchScore >= (record.matchThreshold || 80) ? '✅ 达标' : '❌ 未达标' }}
          </a-tag>
          <span v-else>-</span>
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button type="link" size="small" @click="viewMatchDetail(record)" :disabled="record.matchScore == null">
              查看匹配详情
            </a-button>
            <a-button type="link" size="small" @click="openThresholdModal(record)" :disabled="record.matchScore == null">
              调整阈值
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <!-- Threshold Override Modal -->
    <a-modal v-model:open="thresholdVisible" title="调整匹配阈值" @ok="handleThresholdSubmit" :confirmLoading="thresholdLoading">
      <a-form :model="thresholdForm" layout="vertical">
        <a-form-item label="候选人">
          <span>{{ thresholdForm.candidateName }}</span>
        </a-form-item>
        <a-form-item label="当前匹配分">
          <span>{{ thresholdForm.matchScore }}分</span>
        </a-form-item>
        <a-form-item label="当前阈值">
          <span>{{ thresholdForm.currentThreshold }}分</span>
        </a-form-item>
        <a-form-item label="新阈值">
          <a-input-number
            v-model:value="thresholdForm.newThreshold"
            :min="0"
            :max="100"
            style="width: 100%"
          />
        </a-form-item>
        <a-form-item label="恢复默认值">
          <a-switch v-model:checked="thresholdForm.useDefault" @change="onUseDefaultChange" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { resumeApi } from '@/api/resume'
import { jobPostApi } from '@/api/jobpost'

const router = useRouter()
const loading = ref(false)
const collecting = ref(false)
const dataSource = ref([])
const jobPosts = ref([])
const sourcesLoading = ref(false)
const sourceData = ref([])
const pagination = ref({
  current: 1,
  pageSize: 20,
  total: 0
})

// Filters
const filterJobPostId = ref(null)
const filterChannel = ref(null)
const filterMinScore = ref(null)
const filterMaxScore = ref(null)

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '姓名', dataIndex: 'candidateName', key: 'candidateName' },
  { title: '目标职位', dataIndex: 'jobPostId', key: 'jobPostId', width: 150 },
  { title: '匹配度', key: 'matchScore', width: 200 },
  { title: '达标状态', key: 'matchStatus', width: 100 },
  { title: '渠道', key: 'source', width: 100 },
  { title: '更新时间', dataIndex: 'createdAt', key: 'createdAt', width: 180 },
  { title: '操作', key: 'action', width: 200, fixed: 'right' }
]

const sourceColumns = [
  { title: '渠道', dataIndex: 'channel', key: 'channel', width: 120 },
  { title: '状态', key: 'isActive', width: 80 },
  { title: '最后同步', dataIndex: 'lastSyncAt', key: 'lastSyncAt', width: 180 },
  { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt', width: 180 }
]

// Threshold modal
const thresholdVisible = ref(false)
const thresholdLoading = ref(false)
const thresholdForm = reactive({
  matchId: null,
  candidateName: '',
  matchScore: 0,
  currentThreshold: 0,
  newThreshold: 80,
  useDefault: false
})

function getSourceColor(source) {
  const colors = {
    Boss: 'purple',
    Zhaopin: 'blue',
    Lagou: 'cyan',
    Liepin: 'magenta',
    ZhiLian: 'blue',
    QianCheng: 'green',
    Manual: 'default'
  }
  return colors[source] || 'default'
}

function getSourceText(source) {
  const texts = {
    Boss: 'Boss直聘',
    Zhaopin: '智联招聘',
    Lagou: '拉勾网',
    Liepin: '猎聘网',
    ZhiLian: '智联招聘',
    QianCheng: '前程无忧',
    Manual: '手动录入'
  }
  return texts[source] || source
}

function getScoreStatus(score, threshold) {
  if (score == null) return 'normal'
  const t = threshold || 80
  return score >= t ? 'success' : 'exception'
}

function getScoreColor(score, threshold) {
  if (score == null) return '#d9d9d9'
  const t = threshold || 80
  return score >= t ? '#52c41a' : '#ff4d4f'
}

function getMatchStatusColor(score, threshold) {
  if (score == null) return 'default'
  const t = threshold || 80
  return score >= t ? 'success' : 'error'
}

function getJobPostTitle(jobPostId) {
  if (!jobPostId) return '-'
  const job = jobPosts.value.find(j => j.id === jobPostId)
  return job ? job.title : `#${jobPostId}`
}

async function fetchJobPosts() {
  try {
    const res = await jobPostApi.list({ pageSize: 100 })
    jobPosts.value = res.data?.items || []
  } catch (e) {
    console.error(e)
  }
}

async function fetchData() {
  loading.value = true
  try {
    const res = await resumeApi.list({
      jobPostId: filterJobPostId.value,
      channel: filterChannel.value,
      minScore: filterMinScore.value,
      maxScore: filterMaxScore.value,
      page: pagination.value.current,
      pageSize: pagination.value.pageSize
    })
    const items = res.data?.items || []
    // Attach jobPostTitle for display
    items.forEach(item => {
      item.jobPostTitle = getJobPostTitle(item.jobPostId)
    })
    dataSource.value = items
    pagination.value.total = res.data?.total || 0
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  pagination.value.current = 1
  fetchData()
}

function handleReset() {
  filterJobPostId.value = null
  filterChannel.value = null
  filterMinScore.value = null
  filterMaxScore.value = null
  handleSearch()
}

function handleTableChange(pag) {
  pagination.value.current = pag.current
  pagination.value.pageSize = pag.pageSize
  fetchData()
}

function viewMatchDetail(record) {
  // Find the match result ID - we need to get it from somewhere
  // For now, we'll use jobPostId + resumeId to look up
  router.push(`/matches/${record.jobPostId || 0}?resumeId=${record.id}`)
}

function openThresholdModal(record) {
  thresholdForm.matchId = record.id // Using resume id as match id for now, but we need actual match id
  thresholdForm.candidateName = record.candidateName
  thresholdForm.matchScore = record.matchScore
  thresholdForm.currentThreshold = record.matchThreshold || 80
  thresholdForm.newThreshold = record.matchThreshold || 80
  thresholdForm.useDefault = false
  thresholdVisible.value = true
}

function onUseDefaultChange(checked) {
  if (checked) {
    thresholdForm.newThreshold = null
  } else {
    thresholdForm.newThreshold = thresholdForm.currentThreshold
  }
}

async function handleThresholdSubmit() {
  thresholdLoading.value = true
  try {
    await resumeApi.overrideMatchThreshold(thresholdForm.matchId, thresholdForm.newThreshold)
    message.success('阈值调整成功')
    thresholdVisible.value = false
    fetchData()
  } catch (e) {
    console.error(e)
    message.error('阈值调整失败')
  } finally {
    thresholdLoading.value = false
  }
}

async function handleCollectNow() {
  collecting.value = true
  try {
    await resumeApi.collectNow()
    message.success('采集任务已触发')
    fetchSources()
  } catch (e) {
    console.error(e)
    message.error('触发采集失败')
  } finally {
    collecting.value = false
  }
}

async function fetchSources() {
  sourcesLoading.value = true
  try {
    const res = await resumeApi.getSources()
    sourceData.value = res.data || []
  } catch (e) {
    console.error(e)
  } finally {
    sourcesLoading.value = false
  }
}

function formatDateTime(dateStr) {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN')
}

onMounted(() => {
  fetchJobPosts()
  fetchData()
  fetchSources()
})
</script>

<style scoped>
.resume-list {
  padding: 24px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.page-header-left h1 {
  margin: 0 0 4px 0;
  font-size: 24px;
}

.page-header-left p {
  margin: 0;
  color: #666;
}

.page-header-right {
  display: flex;
  gap: 8px;
}

.source-status-section {
  background: #fafafa;
  padding: 16px;
  border-radius: 4px;
  margin-bottom: 16px;
}

.section-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  font-weight: 500;
}

.filter-bar {
  background: #fafafa;
  padding: 16px;
  border-radius: 4px;
  margin-bottom: 16px;
}

.score-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.score-text {
  font-size: 12px;
  color: #666;
}

.no-match {
  color: #999;
  font-style: italic;
}
</style>