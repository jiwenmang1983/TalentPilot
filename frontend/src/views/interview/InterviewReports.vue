<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>📊 面试报告</h1>
      <p>AI生成的面试评估报告</p>
    </div>
  </div>

  <div class="interview-reports">
    <div class="toolbar">
      <a-space wrap>
        <a-button type="primary" @click="batchExportExcel" :disabled="selectedRowKeys.length === 0">
          批量导出Excel
        </a-button>
        <a-select
          v-model:value="filterRecommendation"
          placeholder="录用建议"
          style="width: 120px"
          allow-clear
          @change="handleSearch"
        >
          <a-select-option value="StrongHire">强烈建议</a-select-option>
          <a-select-option value="Hire">建议录用</a-select-option>
          <a-select-option value="Hold">观望</a-select-option>
          <a-select-option value="Reject">不推荐</a-select-option>
        </a-select>
        <a-input-number
          v-model:value="filterMinScore"
          placeholder="最低分"
          :min="0"
          :max="100"
          style="width: 100px"
          @change="handleSearch"
        />
        <span>~</span>
        <a-input-number
          v-model:value="filterMaxScore"
          placeholder="最高分"
          :min="0"
          :max="100"
          style="width: 100px"
          @change="handleSearch"
        />
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
    </div>

    <a-table
      :columns="columns"
      :data-source="reports"
      :loading="loading"
      :pagination="pagination"
      :row-selection="{ selectedRowKeys, onChange: onSelectChange }"
      @change="handleTableChange"
      row-key="id"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'overallScore'">
          <a-space>
            <a-rate :value="Math.round(record.overallScore / 20)" disabled :count="5" />
            <span>{{ record.overallScore }}</span>
          </a-space>
        </template>
        <template v-else-if="column.key === 'recommendation'">
          <a-tag :color="recommendationMap[record.recommendation]?.color">
            {{ recommendationMap[record.recommendation]?.text || record.recommendation }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'createdAt'">
          {{ formatDateTime(record.createdAt) }}
        </template>
        <template v-else-if="column.key === 'actions'">
          <a-space>
            <a-button type="link" size="small" @click="handleView(record)">
              查看详情
            </a-button>
            <a-button type="link" size="small" @click="exportPdf(record.id)">
              PDF
            </a-button>
            <a-button type="link" size="small" @click="exportExcel(record.id)">
              Excel
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <!-- Detail Modal -->
    <a-modal
      v-model:open="showDetailModal"
      title="面试报告详情"
      :footer="null"
      width="700px"
    >
      <template v-if="currentRecord">
        <div class="report-header">
          <a-row :gutter="16">
            <a-col :span="12">
              <div class="stat-item">
                <div class="stat-label">候选人</div>
                <div class="stat-value">{{ currentRecord.candidateName }}</div>
              </div>
            </a-col>
            <a-col :span="12">
              <div class="stat-item">
                <div class="stat-label">应聘职位</div>
                <div class="stat-value">{{ currentRecord.jobPostTitle }}</div>
              </div>
            </a-col>
          </a-row>
        </div>

        <a-divider />

        <a-row :gutter="16" class="score-section">
          <a-col :span="8">
            <a-card size="small">
              <div class="score-card">
                <div class="score-label">综合评分</div>
                <div class="score-value large">{{ currentRecord.overallScore }}</div>
                <a-tag :color="recommendationMap[currentRecord.recommendation]?.color" style="margin-top: 8px">
                  {{ recommendationMap[currentRecord.recommendation]?.text || currentRecord.recommendation }}
                </a-tag>
              </div>
            </a-card>
          </a-col>
          <a-col :span="16">
            <a-card size="small" title="维度评分">
              <div class="dimension-scores">
                <div
                  v-for="(score, dimension) in currentRecord.dimensionScores"
                  :key="dimension"
                  class="dimension-item"
                >
                  <span class="dimension-name">{{ dimension }}</span>
                  <a-progress
                    :percent="score"
                    :stroke-color="score >= 80 ? '#52c41a' : score >= 60 ? '#faad14' : '#ff4d4f'"
                    size="small"
                    style="width: 120px"
                  />
                  <span class="dimension-score">{{ score }}</span>
                </div>
              </div>
            </a-card>
          </a-col>
        </a-row>

        <a-divider>AI评语</a-divider>
        <div class="ai-comments">
          <pre>{{ currentRecord.aiComments }}</pre>
        </div>

        <a-row :gutter="16" style="margin-top: 16px">
          <a-col :span="12">
            <div v-if="currentRecord.highlights?.length > 0">
              <strong>亮点：</strong>
              <a-tag
                v-for="highlight in currentRecord.highlights"
                :key="highlight"
                color="success"
                style="margin: 4px"
              >
                {{ highlight }}
              </a-tag>
            </div>
          </a-col>
          <a-col :span="12">
            <div v-if="currentRecord.concerns?.length > 0">
              <strong>风险提示：</strong>
              <a-tag
                v-for="concern in currentRecord.concerns"
                :key="concern"
                color="warning"
                style="margin: 4px"
              >
                {{ concern }}
              </a-tag>
            </div>
          </a-col>
        </a-row>

        <a-divider>HR备注</a-divider>
        <a-textarea
          v-model:value="hrNotes"
          placeholder="添加HR备注..."
          :rows="3"
          :disabled="!canEditNotes"
        />
        <div style="margin-top: 8px; text-align: right">
          <a-button
            v-if="canEditNotes"
            type="primary"
            size="small"
            @click="handleSaveNotes"
            :loading="savingNotes"
          >
            保存备注
          </a-button>
        </div>

        <a-divider>面试Transcript回顾</a-divider>
        <div class="transcript-section">
          <p v-if="!currentRecord.transcript" class="no-transcript">暂无面试记录</p>
          <pre v-else>{{ currentRecord.transcript }}</pre>
        </div>
      </template>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { message } from 'ant-design-vue'
import { SearchOutlined } from '@ant-design/icons-vue'
import { interviewReportApi } from '@/api/interviewReport'
import dayjs from 'dayjs'

const recommendationMap = {
  'StrongHire': { color: 'success', text: '强烈建议' },
  'Hire': { color: 'processing', text: '建议录用' },
  'Hold': { color: 'warning', text: '观望' },
  'Reject': { color: 'error', text: '不推荐' }
}

const reports = ref([])
const loading = ref(false)
const showDetailModal = ref(false)
const currentRecord = ref(null)
const hrNotes = ref('')
const savingNotes = ref(false)
const selectedRowKeys = ref([])

const filterRecommendation = ref(null)
const filterMinScore = ref(null)
const filterMaxScore = ref(null)
const dateRange = ref(null)

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
  showSizeChanger: true,
  showTotal: (total) => `共 ${total} 条`
})

const canEditNotes = computed(() => {
  // Check if user has HR role
  const roles = JSON.parse(localStorage.getItem('userInfo') || '{}').roles || []
  return roles.some(r => ['admin', 'hr'].includes(r.toLowerCase()))
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '候选人', dataIndex: 'candidateName', key: 'candidateName' },
  { title: '应聘职位', dataIndex: 'jobPostTitle', key: 'jobPostTitle' },
  { title: '综合评分', key: 'overallScore', width: 180 },
  { title: '评级', key: 'recommendation', width: 100 },
  { title: '生成时间', key: 'createdAt', width: 160 },
  { title: '操作', key: 'actions', width: 120, fixed: 'right' }
]

function formatDateTime(date) {
  return dayjs(date).format('YYYY-MM-DD HH:mm')
}

async function fetchReports() {
  loading.value = true
  try {
    const params = {
      page: pagination.current,
      pageSize: pagination.pageSize,
      recommendation: filterRecommendation.value || undefined,
      minScore: filterMinScore.value || undefined,
      maxScore: filterMaxScore.value || undefined,
      dateFrom: dateRange.value?.[0]?.toISOString(),
      dateTo: dateRange.value?.[1]?.toISOString()
    }
    const res = await interviewReportApi.list(params)
    reports.value = res.data?.items || res.items || []
    pagination.total = res.data?.total || res.total || 0
  } catch (e) {
    message.error('获取报告列表失败')
  } finally {
    loading.value = false
  }
}

async function handleView(record) {
  try {
    const res = await interviewReportApi.getById(record.id)
    const report = res.data || res
    currentRecord.value = report
    hrNotes.value = report.hrNotes || ''
    showDetailModal.value = true
  } catch (e) {
    message.error('获取报告详情失败')
  }
}

async function handleSaveNotes() {
  if (!currentRecord.value) return

  savingNotes.value = true
  try {
    await interviewReportApi.updateNotes(currentRecord.value.id, hrNotes.value)
    message.success('备注保存成功')
  } catch (e) {
    message.error('保存失败')
  } finally {
    savingNotes.value = false
  }
}

function handleSearch() {
  pagination.current = 1
  fetchReports()
}

function handleTableChange(pag) {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  fetchReports()
}

function onSelectChange(keys) {
  selectedRowKeys.value = keys
}

async function exportPdf(id) {
  const token = localStorage.getItem('accessToken')
  try {
    const resp = await fetch(`/api/interview-reports/${id}/export-pdf`, {
      headers: { 'Authorization': `Bearer ${token}` }
    })
    if (!resp.ok) throw new Error('Export failed')
    const blob = await resp.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `面试报告_${id}.pdf`
    a.click()
    URL.revokeObjectURL(url)
  } catch (e) {
    message.error('PDF导出失败')
  }
}

async function exportExcel(id) {
  const token = localStorage.getItem('accessToken')
  try {
    const resp = await fetch(`/api/interview-reports/${id}/export-excel`, {
      headers: { 'Authorization': `Bearer ${token}` }
    })
    if (!resp.ok) throw new Error('Export failed')
    const blob = await resp.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `面试报告_${id}.xlsx`
    a.click()
    URL.revokeObjectURL(url)
  } catch (e) {
    message.error('Excel导出失败')
  }
}

async function batchExportExcel() {
  if (selectedRowKeys.value.length === 0) {
    message.warning('请先选择要导出的报告')
    return
  }
  const token = localStorage.getItem('accessToken')
  try {
    const resp = await fetch('/api/interview-reports/export-excel-batch', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(selectedRowKeys.value)
    })
    if (!resp.ok) throw new Error('Export failed')
    const blob = await resp.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `批量面试报告_${new Date().toISOString().slice(0,10).replace(/-/g,'')}.xlsx`
    a.click()
    URL.revokeObjectURL(url)
    message.success('批量导出成功')
  } catch (e) {
    message.error('批量Excel导出失败')
  }
}

onMounted(() => {
  fetchReports()
})
</script>

<style scoped>
.interview-reports {
  height: 100%;
}

.toolbar {
  margin-bottom: 16px;
}

.report-header {
  padding: 8px 0;
}

.stat-item {
  text-align: center;
}

.stat-label {
  color: #999;
  font-size: 12px;
}

.stat-value {
  font-size: 16px;
  font-weight: 500;
  margin-top: 4px;
}

.score-section {
  margin-top: 16px;
}

.score-card {
  text-align: center;
}

.score-label {
  color: #999;
  font-size: 12px;
}

.score-value {
  font-size: 32px;
  font-weight: bold;
  color: #1890ff;
}

.score-value.large {
  font-size: 36px;
}

.dimension-scores {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.dimension-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.dimension-name {
  width: 80px;
  font-size: 13px;
}

.dimension-score {
  width: 30px;
  text-align: right;
  font-size: 13px;
  color: #666;
}

.ai-comments {
  background: #f5f5f5;
  padding: 16px;
  border-radius: 4px;
}

.ai-comments pre {
  white-space: pre-wrap;
  word-break: break-word;
  margin: 0;
  font-family: inherit;
}

.transcript-section {
  background: #fafafa;
  padding: 16px;
  border-radius: 4px;
  max-height: 300px;
  overflow-y: auto;
}

.transcript-section pre {
  white-space: pre-wrap;
  word-break: break-word;
  margin: 0;
  font-family: inherit;
}

.no-transcript {
  color: #999;
  text-align: center;
  margin: 0;
}
</style>
