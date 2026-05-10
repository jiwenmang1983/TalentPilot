<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>🎯 智能匹配</h1>
      <p>职位与候选人8维度AI智能匹配结果</p>
    </div>
    <div class="page-header-right">
      <a-button type="primary" @click="handleBatchMatch" :loading="batchMatching">
        <template #icon><AimOutlined /></template>
        重新匹配
      </a-button>
    </div>
  </div>

  <div class="match-result-list">
    <!-- Filters -->
    <div class="filter-row">
      <a-space wrap>
        <a-select
          v-model:value="filterJobPostId"
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
        <a-select
          v-model:value="filterStatus"
          placeholder="筛选状态"
          style="width: 120px"
          allowClear
          @change="handleSearch"
        >
          <a-select-option value="Pending">待查看</a-select-option>
          <a-select-option value="Reviewed">已查看</a-select-option>
          <a-select-option value="Accepted">录用</a-select-option>
          <a-select-option value="Rejected">不合适</a-select-option>
        </a-select>
        <a-input-number
          v-model:value="filterScoreMin"
          placeholder="最低分"
          style="width: 100px"
          :min="0"
          :max="100"
          @change="handleSearch"
        />
        <span style="color: #999; line-height: 32px;">-</span>
        <a-input-number
          v-model:value="filterScoreMax"
          placeholder="最高分"
          style="width: 100px"
          :min="0"
          :max="100"
          @change="handleSearch"
        />
      </a-space>
    </div>

    <!-- Main Table -->
    <a-table
      :columns="columns"
      :dataSource="dataSource"
      :loading="loading"
      :pagination="pagination"
      @change="handleTableChange"
      :expandable="{ expandedRowRender: expandedRowRender }"
      rowKey="id"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'candidateName'">
          <span>{{ record.candidateName || '-' }}</span>
        </template>
        <template v-else-if="column.key === 'jobTitle'">
          <span>{{ record.jobTitle || '-' }}</span>
        </template>
        <template v-else-if="column.key === 'score'">
          <div v-if="record.dimensionScores">
            <a-progress
              :percent="record.score"
              :status="getScoreStatus(record.score, record.matchThreshold)"
              size="small"
              :stroke-color="getScoreColor(record.score, record.matchThreshold)"
            />
            <span style="font-size: 12px; color: #666;">{{ record.score }}分</span>
          </div>
          <span v-else>{{ record.score || 0 }}分</span>
        </template>
        <template v-else-if="column.key === 'skillScore'">
          <span v-if="record.dimensionScores" :style="{ color: getScoreColor(record.dimensionScores.skillScore, record.matchThreshold) }">
            {{ record.dimensionScores.skillScore || '-' }}
          </span>
          <span v-else>-</span>
        </template>
        <template v-else-if="column.key === 'experienceScore'">
          <span v-if="record.dimensionScores" :style="{ color: getScoreColor(record.dimensionScores.experienceScore, record.matchThreshold) }">
            {{ record.dimensionScores.experienceScore || '-' }}
          </span>
          <span v-else>-</span>
        </template>
        <template v-else-if="column.key === 'educationScore'">
          <span v-if="record.dimensionScores" :style="{ color: getScoreColor(record.dimensionScores.educationScore, record.matchThreshold) }">
            {{ record.dimensionScores.educationScore || '-' }}
          </span>
          <span v-else>-</span>
        </template>
        <template v-else-if="column.key === 'status'">
          <a-tag :color="getStatusColor(record.status)">
            {{ getStatusText(record.status) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a @click="handleViewDetail(record)">详情</a>
            <a-divider type="vertical" />
            <a-dropdown>
              <a>操作</a>
              <template #overlay>
                <a-menu>
                  <a-menu-item @click="handleUpdateStatus(record.id, 'Reviewed')" v-if="record.status === 'Pending'">
                    标记已查看
                  </a-menu-item>
                  <a-menu-item @click="handleUpdateStatus(record.id, 'Accepted')">
                    录用
                  </a-menu-item>
                  <a-menu-item @click="handleUpdateStatus(record.id, 'Rejected')">
                    不合适
                  </a-menu-item>
                </a-menu>
              </template>
            </a-dropdown>
          </a-space>
        </template>
      </template>
    </a-table>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { AimOutlined } from '@ant-design/icons-vue'
import { matchResultApi } from '@/api/matchResult'
import { jobPostApi } from '@/api/jobpost'
import { message } from 'ant-design-vue'

const router = useRouter()
const loading = ref(false)
const batchMatching = ref(false)
const filterJobPostId = ref(null)
const filterStatus = ref(null)
const filterScoreMin = ref(null)
const filterScoreMax = ref(null)
const dataSource = ref([])
const jobPosts = ref([])
const pagination = ref({
  current: 1,
  pageSize: 20,
  total: 0
})

const columns = [
  { title: '候选人', key: 'candidateName', width: 120 },
  { title: '职位', key: 'jobTitle', width: 180 },
  { title: '综合分', key: 'score', width: 160 },
  { title: '技能分', key: 'skillScore', width: 80 },
  { title: '年限分', key: 'experienceScore', width: 80 },
  { title: '学历分', key: 'educationScore', width: 80 },
  { title: '状态', key: 'status', width: 100 },
  { title: '操作', key: 'action', width: 120, fixed: 'right' }
]

// 展开行渲染
const expandedRowRender = (record) => {
  if (!record.dimensionScores) {
    return h('div', '暂无维度详情')
  }
  const dims = [
    { key: 'skillScore', label: '技能' },
    { key: 'experienceScore', label: '年限' },
    { key: 'educationScore', label: '学历' },
    { key: 'industryScore', label: '行业' },
    { key: 'levelScore', label: '级别' },
    { key: 'salaryScore', label: '薪资' },
    { key: 'locationScore', label: '地区' },
    { key: 'turnoverScore', label: '稳定性' }
  ]
  return h('div', { class: 'expanded-dims' }, [
    dims.map(d => h('div', { class: 'dim-row', key: d.key }, [
      h('span', { class: 'dim-label' }, d.label),
      h('a-progress', {
        percent: record.dimensionScores[d.key] || 0,
        size: 'small',
        status: getScoreStatus(record.dimensionScores[d.key], record.matchThreshold),
        strokeColor: getScoreColor(record.dimensionScores[d.key], record.matchThreshold),
        style: { width: '200px' }
      }),
      h('span', { class: 'dim-value' }, `${record.dimensionScores[d.key] || 0}分`)
    ])),
    record.matchText ? h('div', { class: 'match-text-section' }, [
      h('div', { class: 'section-title' }, '匹配分析'),
      h('div', { class: 'match-text-content' }, record.matchText)
    ]) : null
  ])
}

function getScoreStatus(score, threshold) {
  if (!threshold) return score >= 80 ? 'success' : 'exception'
  return score >= threshold ? 'success' : 'exception'
}

function getScoreColor(score, threshold) {
  if (!threshold) return score >= 80 ? '#52c41a' : '#ff4d4f'
  return score >= threshold ? '#52c41a' : '#ff4d4f'
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
    const params = {
      page: pagination.value.current,
      pageSize: pagination.value.pageSize
    }
    if (filterJobPostId.value) params.jobPostId = filterJobPostId.value
    if (filterStatus.value) params.status = filterStatus.value
    if (filterScoreMin.value !== null) params.scoreMin = filterScoreMin.value
    if (filterScoreMax.value !== null) params.scoreMax = filterScoreMax.value

    const res = await matchResultApi.list(params)
    const items = res.data?.items || []
    
    // 解析 dimensionScores 和 dimensionWeights JSON
    dataSource.value = items.map(item => {
      if (item.dimensionScores && typeof item.dimensionScores === 'string') {
        try {
          item.dimensionScores = JSON.parse(item.dimensionScores)
        } catch (e) {
          console.error('Failed to parse dimensionScores:', e)
        }
      }
      if (item.dimensionWeights && typeof item.dimensionWeights === 'string') {
        try {
          item.dimensionWeights = JSON.parse(item.dimensionWeights)
        } catch (e) {
          console.error('Failed to parse dimensionWeights:', e)
        }
      }
      return item
    })
    pagination.value.total = res.data?.total || 0
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function fetchJobPosts(value = '') {
  try {
    const res = await jobPostApi.list({ keyword: value, pageSize: 100 })
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

function handleJobPostSearch(value) {
  if (value) {
    fetchJobPosts(value)
  }
}

function handleViewDetail(record) {
  router.push(`/matches/${record.id}`)
}

async function handleUpdateStatus(id, status) {
  try {
    await matchResultApi.updateStatus(id, status)
    message.success('状态更新成功')
    fetchData()
  } catch (e) {
    console.error(e)
  }
}

async function handleBatchMatch() {
  if (!filterJobPostId.value) {
    message.warning('请先选择一个职位进行匹配')
    return
  }
  batchMatching.value = true
  try {
    await matchResultApi.batchMatch(filterJobPostId.value)
    message.success('匹配任务已启动')
    fetchData()
  } catch (e) {
    console.error(e)
  } finally {
    batchMatching.value = false
  }
}

onMounted(() => {
  fetchJobPosts()
  fetchData()
})
</script>

<script>
import { h } from 'vue'
export default {
  name: 'MatchResultList'
}
</script>

<style scoped>
.match-result-list {
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

.filter-row {
  margin-bottom: 16px;
}

.expanded-dims {
  padding: 8px 0;
}

.dim-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 8px;
}

.dim-label {
  width: 60px;
  color: #666;
}

.dim-value {
  width: 50px;
  color: #333;
}

.match-text-section {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #f0f0f0;
}

.section-title {
  font-weight: 500;
  margin-bottom: 8px;
  color: #333;
}

.match-text-content {
  color: #666;
  line-height: 1.6;
  white-space: pre-wrap;
}
</style>