<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>📋 我的面试</h1>
      <p>查看您的面试记录和报告</p>
    </div>
  </div>

  <div class="candidate-dashboard">
    <!-- Query Form -->
    <a-card class="query-card">
      <a-space wrap>
        <a-input
          v-model:value="sessionToken"
          placeholder="输入面试会话码"
          style="width: 260px"
          @pressEnter="handleQuery"
        />
        <a-button type="primary" @click="handleQuery" :loading="loading">
          查询
        </a-button>
      </a-space>
    </a-card>

    <!-- Results -->
    <a-card v-if="result" class="result-card">
      <div class="session-info">
        <a-descriptions :column="2" bordered size="small">
          <a-descriptions-item label="会话码">{{ result.sessionToken || result.sessionId }}</a-descriptions-item>
          <a-descriptions-item label="状态">
            <a-tag :color="statusColor(result.status)">{{ statusText(result.status) }}</a-tag>
          </a-descriptions-item>
          <a-descriptions-item label="职位">{{ result.jobTitle || '-' }}</a-descriptions-item>
          <a-descriptions-item label="面试时长">{{ result.interviewDuration || 20 }}分钟</a-descriptions-item>
          <a-descriptions-item label="开始时间">{{ formatTime(result.startTime) }}</a-descriptions-item>
          <a-descriptions-item label="结束时间">{{ formatTime(result.endTime) || '进行中' }}</a-descriptions-item>
        </a-descriptions>
      </div>

      <!-- Report Section -->
      <div v-if="report" class="report-section">
        <h3>📊 评估报告</h3>
        <a-descriptions :column="2" bordered size="small">
          <a-descriptions-item label="综合评分">
            <a-tag :color="scoreColor(report.overallScore)">{{ report.overallScore || 0 }}分</a-tag>
          </a-descriptions-item>
          <a-descriptions-item label="录用建议">
            <a-tag :color="recommendationColor(report.recommendation)">
              {{ recommendationText(report.recommendation) }}
            </a-tag>
          </a-descriptions-item>
          <a-descriptions-item label="技能评估" :span="2">{{ report.skillScore || '-' }}</a-descriptions-item>
          <a-descriptions-item label="沟通评估" :span="2">{{ report.communicationScore || '-' }}</a-descriptions-item>
        </a-descriptions>
        <div class="report-actions">
          <a-button type="primary" @click="exportPDF" :loading="exportingPdf">
            导出PDF
          </a-button>
          <a-button @click="exportExcel" :loading="exportingExcel">
            导出Excel
          </a-button>
        </div>
      </div>

      <!-- No Report Yet -->
      <div v-else-if="result.status === 'Completed'" class="no-report">
        <a-empty description="报告生成中，请稍后再来查询" />
      </div>
    </a-card>

    <!-- No Result -->
    <a-card v-else-if="searched && !result">
      <a-empty description="未找到该面试记录" />
    </a-card>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { message } from 'ant-design-vue'
import axios from 'axios'

const sessionToken = ref('')
const loading = ref(false)
const searched = ref(false)
const result = ref(null)
const report = ref(null)
const exportingPdf = ref(false)
const exportingExcel = ref(false)

async function handleQuery() {
  if (!sessionToken.value.trim()) {
    message.warning('请输入会话码')
    return
  }
  loading.value = true
  searched.value = true
  result.value = null
  report.value = null

  try {
    // Try to get report by session token
    const sessionRes = await axios.get(`/api/interview-reports/by-session/${sessionToken.value}`)
    if (sessionRes.data) {
      result.value = sessionRes.data
      report.value = sessionRes.data
    }
  } catch (e) {
    if (e.response?.status === 404) {
      result.value = null
    } else {
      message.error('查询失败: ' + (e.response?.data?.message || e.message))
    }
  } finally {
    loading.value = false
  }
}

async function exportPDF() {
  if (!report.value?.id) return
  exportingPdf.value = true
  try {
    const res = await axios.get(`/api/interview-reports/${report.value.id}/export-pdf`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = `面试报告_${sessionToken.value}.pdf`
    a.click()
    URL.revokeObjectURL(url)
    message.success('PDF导出成功')
  } catch (e) {
    message.error('PDF导出失败')
  } finally {
    exportingPdf.value = false
  }
}

async function exportExcel() {
  if (!report.value?.id) return
  exportingExcel.value = true
  try {
    const res = await axios.get(`/api/interview-reports/${report.value.id}/export-excel`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = `面试报告_${sessionToken.value}.xlsx`
    a.click()
    URL.revokeObjectURL(url)
    message.success('Excel导出成功')
  } catch (e) {
    message.error('Excel导出失败')
  } finally {
    exportingExcel.value = false
  }
}

function statusColor(s) {
  const map = { Scheduled: 'blue', InProgress: 'processing', Completed: 'green', Abandoned: 'red', Cancelled: 'default' }
  return map[s] || 'default'
}
function statusText(s) {
  const map = { Scheduled: '已安排', InProgress: '进行中', Completed: '已完成', Abandoned: '已放弃', Cancelled: '已取消' }
  return map[s] || s || '-'
}
function scoreColor(score) {
  if (!score) return 'default'
  if (score >= 80) return 'green'
  if (score >= 60) return 'orange'
  return 'red'
}
function recommendationColor(r) {
  const map = { StrongHire: 'green', Hire: 'cyan', Hold: 'orange', Reject: 'red' }
  return map[r] || 'default'
}
function recommendationText(r) {
  const map = { StrongHire: '强烈建议', Hire: '建议录用', Hold: '观望', Reject: '不推荐' }
  return map[r] || r || '-'
}
function formatTime(t) {
  if (!t) return '-'
  return new Date(t).toLocaleString('zh-CN', { timeZone: 'Asia/Shanghai' })
}
</script>

<style scoped>
.candidate-dashboard {
  max-width: 800px;
  margin: 0 auto;
}
.query-card {
  margin-bottom: 16px;
}
.result-card {
  margin-bottom: 16px;
}
.session-info {
  margin-bottom: 20px;
}
.report-section {
  margin-top: 20px;
}
.report-section h3 {
  margin-bottom: 12px;
  font-size: 16px;
}
.report-actions {
  margin-top: 16px;
  display: flex;
  gap: 8px;
}
.no-report {
  margin-top: 20px;
  text-align: center;
}
</style>
