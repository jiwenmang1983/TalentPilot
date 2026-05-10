<template>
  <div class="match-result-detail">
    <!-- Page Header -->
    <div class="page-header">
      <div class="page-header-left">
        <h1>🔍 匹配详情</h1>
        <p>8维度智能匹配详细分析</p>
      </div>
      <div class="page-header-right">
        <a-button @click="$router.push('/matches')">
          <template #icon><ArrowLeftOutlined /></template>
          返回列表
        </a-button>
      </div>
    </div>

    <a-spin :spinning="loading" v-if="match">
      <!-- Top Banner -->
      <a-card class="score-banner">
        <div class="banner-content">
          <div class="banner-left">
            <div class="candidate-info">
              <h2>{{ match.candidateName || '候选人' }}</h2>
              <p class="job-title">{{ match.jobTitle || '职位' }}</p>
            </div>
          </div>
          <div class="banner-right">
            <div class="score-display">
              <span class="score-label">综合匹配分</span>
              <span class="score-value">{{ match.score || 0 }}</span>
              <span class="score-max">/ 100</span>
            </div>
            <a-tag
              :color="match.score >= match.matchThreshold ? 'success' : 'error'"
              class="threshold-badge"
            >
              {{ match.score >= match.matchThreshold ? '✅ 达标' : '❌ 未达标' }}
            </a-tag>
            <span class="threshold-hint">阈值: {{ match.matchThreshold || 0 }}分</span>
          </div>
        </div>
      </a-card>

      <!-- 8 Dimension Scores -->
      <a-card title="📊 8维度评分详情" class="dimensions-card">
        <div class="dimensions-list">
          <div
            v-for="dim in dimensionDefs"
            :key="dim.key"
            class="dimension-item"
          >
            <div class="dimension-label">
              <span class="dim-name">{{ dim.label }}</span>
              <span class="dim-weight" v-if="dimensionWeights && dimensionWeights[dim.weightKey]">
                (权重: {{ dimensionWeights[dim.weightKey] }}%)
              </span>
            </div>
            <div class="dimension-bar">
              <a-progress
                :percent="dimensionScores[dim.key] || 0"
                :status="getScoreStatus(dimensionScores[dim.key])"
                :stroke-color="getScoreColor(dimensionScores[dim.key])"
                :show-info="false"
              />
            </div>
            <div class="dimension-value">
              <span :style="{ color: getScoreColor(dimensionScores[dim.key]) }">
                {{ dimensionScores[dim.key] || 0 }}
              </span>
              <span class="dim-max">分</span>
            </div>
          </div>
        </div>

        <!-- Match Threshold Reference -->
        <div class="threshold-reference">
          <a-divider>参考阈值线</a-divider>
          <div class="threshold-line">
            <span class="threshold-label">匹配达标线:</span>
            <span class="threshold-value">{{ match.matchThreshold || 0 }}分</span>
          </div>
          <p class="threshold-hint-text">各维度分数达到或超过阈值线表示该项匹配达标</p>
        </div>
      </a-card>

      <!-- Match Text Analysis -->
      <a-card title="📝 匹配分析详情" class="analysis-card" v-if="match.matchText">
        <div class="analysis-content">
          <pre>{{ match.matchText }}</pre>
        </div>
      </a-card>

      <!-- Dimension Weights Configuration -->
      <a-card
        title="⚙️ 维度权重配置"
        class="weights-card"
        v-if="dimensionWeights"
      >
        <a-alert type="info" show-icon style="margin-bottom: 16px">
          <template #message>权重说明</template>
          <template #description>调整各维度在综合评分中的权重占比，所有权重总和必须等于100%</template>
        </a-alert>

        <div class="weights-list">
          <div v-for="dim in dimensionDefs" :key="dim.key" class="weight-item">
            <span class="weight-label">{{ dim.label }}</span>
            <a-slider
              v-model:value="localWeights[dim.weightKey]"
              :min="0"
              :max="100"
              :marks="{ 0: '0', 25: '25', 50: '50', 75: '75', 100: '100' }"
              style="flex: 1"
            />
            <span class="weight-value">{{ localWeights[dim.weightKey] }}%</span>
          </div>
        </div>

        <div class="weights-total">
          <span>权重总和:</span>
          <a-tag :color="totalWeight === 100 ? 'success' : 'error'" style="margin-left: 8px">
            {{ totalWeight }}%
          </a-tag>
          <span v-if="totalWeight !== 100" style="color: #ff4d4f; margin-left: 8px">
            (必须等于100%才能保存)
          </span>
        </div>

        <div class="weights-actions">
          <a-button type="primary" :disabled="totalWeight !== 100" :loading="savingWeights" @click="handleSaveWeights">
            保存权重配置
          </a-button>
          <a-button @click="handleResetWeights" style="margin-left: 8px">
            重置
          </a-button>
        </div>
      </a-card>

      <!-- Status Update -->
      <a-card title="🔄 状态管理" class="status-card">
        <div class="status-info">
          <span>当前状态:</span>
          <a-tag :color="getStatusColor(match.status)">
            {{ getStatusText(match.status) }}
          </a-tag>
        </div>
        <div class="status-actions">
          <a-button
            v-if="match.status === 'Pending'"
            @click="handleUpdateStatus('Reviewed')"
          >
            标记已查看
          </a-button>
          <a-button type="primary" color="success" @click="handleUpdateStatus('Accepted')">
            录用
          </a-button>
          <a-button danger @click="handleUpdateStatus('Rejected')">
            不合适
          </a-button>
        </div>
      </a-card>
    </a-spin>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ArrowLeftOutlined } from '@ant-design/icons-vue'
import { matchResultApi } from '@/api/matchResult'
import { message } from 'ant-design-vue'

const route = useRoute()
const loading = ref(false)
const savingWeights = ref(false)
const match = ref(null)
const localWeights = ref({
  skillWeight: 30,
  experienceWeight: 20,
  educationWeight: 15,
  industryWeight: 10,
  levelWeight: 10,
  salaryWeight: 5,
  locationWeight: 5,
  turnoverWeight: 5
})

const dimensionDefs = [
  { key: 'skillScore', label: '技能匹配', weightKey: 'skillWeight' },
  { key: 'experienceScore', label: '年限经验', weightKey: 'experienceWeight' },
  { key: 'educationScore', label: '学历背景', weightKey: 'educationWeight' },
  { key: 'industryScore', label: '行业背景', weightKey: 'industryWeight' },
  { key: 'levelScore', label: '职级水平', weightKey: 'levelWeight' },
  { key: 'salaryScore', label: '薪资期望', weightKey: 'salaryWeight' },
  { key: 'locationScore', label: '地区位置', weightKey: 'locationWeight' },
  { key: 'turnoverScore', label: '工作稳定性', weightKey: 'turnoverWeight' }
]

const dimensionScores = computed(() => {
  if (!match.value?.dimensionScores) return {}
  if (typeof match.value.dimensionScores === 'string') {
    try {
      return JSON.parse(match.value.dimensionScores)
    } catch (e) {
      console.error('Failed to parse dimensionScores:', e)
      return {}
    }
  }
  return match.value.dimensionScores
})

const dimensionWeights = computed(() => {
  if (!match.value?.dimensionWeights) return null
  if (typeof match.value.dimensionWeights === 'string') {
    try {
      return JSON.parse(match.value.dimensionWeights)
    } catch (e) {
      console.error('Failed to parse dimensionWeights:', e)
      return null
    }
  }
  return match.value.dimensionWeights
})

const totalWeight = computed(() => {
  return Object.values(localWeights.value).reduce((sum, v) => sum + (v || 0), 0)
})

function getScoreStatus(score) {
  if (!match.value?.matchThreshold) return score >= 80 ? 'success' : 'exception'
  return score >= match.value.matchThreshold ? 'success' : 'exception'
}

function getScoreColor(score) {
  if (!match.value?.matchThreshold) return score >= 80 ? '#52c41a' : '#ff4d4f'
  return score >= match.value.matchThreshold ? '#52c41a' : '#ff4d4f'
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
    const id = route.params.id
    const res = await matchResultApi.getById(id)
    match.value = res.data || res

    // 初始化权重值
    if (match.value.dimensionWeights) {
      let weights = dimensionWeights.value
      if (weights) {
        localWeights.value = { ...weights }
      }
    }
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function handleUpdateStatus(status) {
  try {
    await matchResultApi.updateStatus(match.value.id, status)
    match.value.status = status
    message.success('状态更新成功')
  } catch (e) {
    console.error(e)
  }
}

async function handleSaveWeights() {
  if (totalWeight.value !== 100) {
    message.warning('权重总和必须等于100%才能保存')
    return
  }
  savingWeights.value = true
  try {
    const weightsJson = JSON.stringify(localWeights.value)
    await matchResultApi.setWeights(match.value.jobPostId, weightsJson)
    match.value.dimensionWeights = localWeights.value
    message.success('权重配置已保存')
  } catch (e) {
    console.error(e)
  } finally {
    savingWeights.value = false
  }
}

function handleResetWeights() {
  if (dimensionWeights.value) {
    localWeights.value = { ...dimensionWeights.value }
  }
}

onMounted(() => {
  fetchData()
})
</script>

<style scoped>
.match-result-detail {
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

.score-banner {
  margin-bottom: 16px;
}

.banner-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.candidate-info h2 {
  margin: 0 0 4px 0;
  font-size: 20px;
}

.job-title {
  margin: 0;
  color: #666;
}

.score-display {
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.score-label {
  font-size: 14px;
  color: #666;
}

.score-value {
  font-size: 48px;
  font-weight: bold;
  color: #1890ff;
}

.score-max {
  font-size: 16px;
  color: #999;
}

.threshold-badge {
  margin-left: 16px;
  font-size: 16px;
  padding: 4px 12px;
}

.threshold-hint {
  margin-left: 12px;
  color: #999;
  font-size: 12px;
}

.dimensions-card {
  margin-bottom: 16px;
}

.dimensions-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.dimension-item {
  display: flex;
  align-items: center;
  gap: 12px;
}

.dimension-label {
  width: 120px;
  display: flex;
  flex-direction: column;
}

.dim-name {
  font-weight: 500;
}

.dim-weight {
  font-size: 12px;
  color: #999;
}

.dimension-bar {
  flex: 1;
  max-width: 400px;
}

.dimension-value {
  width: 60px;
  text-align: right;
}

.dim-max {
  color: #999;
  font-size: 12px;
}

.threshold-reference {
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid #f0f0f0;
}

.threshold-line {
  display: flex;
  align-items: center;
  gap: 8px;
}

.threshold-label {
  color: #666;
}

.threshold-value {
  font-weight: 500;
  color: #1890ff;
}

.threshold-hint-text {
  font-size: 12px;
  color: #999;
  margin-top: 8px;
}

.analysis-card {
  margin-bottom: 16px;
}

.analysis-content pre {
  white-space: pre-wrap;
  word-break: break-word;
  font-family: inherit;
  font-size: 14px;
  line-height: 1.6;
  color: #333;
}

.weights-card {
  margin-bottom: 16px;
}

.weights-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.weight-item {
  display: flex;
  align-items: center;
  gap: 12px;
}

.weight-label {
  width: 80px;
  font-weight: 500;
}

.weight-value {
  width: 50px;
  text-align: right;
  font-weight: 500;
}

.weights-total {
  display: flex;
  align-items: center;
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #f0f0f0;
}

.weights-actions {
  margin-top: 16px;
}

.status-card {
  margin-bottom: 16px;
}

.status-info {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
}

.status-actions {
  display: flex;
  gap: 8px;
}
</style>