<template>
  <div class="match-result-detail">
    <a-spin :spinning="loading" v-if="match">
      <a-card title="匹配结果详情">
        <a-descriptions :column="2" bordered>
          <a-descriptions-item label="匹配ID">{{ match.id }}</a-descriptions-item>
          <a-descriptions-item label="简历ID">{{ match.resumeId }}</a-descriptions-item>
          <a-descriptions-item label="职位ID">{{ match.jobPostId }}</a-descriptions-item>
          <a-descriptions-item label="匹配分数">
            <a-progress :percent="match.score" :status="getScoreStatus(match.score)" />
          </a-descriptions-item>
          <a-descriptions-item label="状态">
            <a-tag :color="getStatusColor(match.status)">
              {{ getStatusText(match.status) }}
            </a-tag>
          </a-descriptions-item>
          <a-descriptions-item label="创建时间">{{ match.createdAt }}</a-descriptions-item>
          <a-descriptions-item label="匹配技能" :span="2">
            <a-tag v-for="skill in parseSkills(match.matchedSkills)" :key="skill" color="success">
              {{ skill }}
            </a-tag>
            <span v-if="!parseSkills(match.matchedSkills).length">-</span>
          </a-descriptions-item>
          <a-descriptions-item label="缺失技能" :span="2">
            <a-tag v-for="skill in parseSkills(match.missingSkills)" :key="skill" color="warning">
              {{ skill }}
            </a-tag>
            <span v-if="!parseSkills(match.missingSkills).length">-</span>
          </a-descriptions-item>
          <a-descriptions-item label="匹配理由" :span="2">
            {{ match.summary || '-' }}
          </a-descriptions-item>
        </a-descriptions>
      </a-card>
    </a-spin>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { matchApi } from '@/api/match'

const route = useRoute()
const loading = ref(false)
const match = ref(null)

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
    const res = await matchApi.list({ page: 1, pageSize: 100 })
    match.value = res.data?.items?.find(m => m.id === parseInt(route.params.id))
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
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
</style>
