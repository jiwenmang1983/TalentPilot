<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>👤 候选人详情</h1>
      <p>候选人完整信息</p>
    </div>
  </div>

  <div class="candidate-detail">
    <a-spin :spinning="loading">
      <a-row :gutter="24" v-if="candidate">
        <a-col :span="16">
          <a-card title="基本信息" class="info-card">
            <a-descriptions :column="2" bordered>
              <a-descriptions-item label="姓名">{{ candidate.name }}</a-descriptions-item>
              <a-descriptions-item label="邮箱">{{ candidate.email }}</a-descriptions-item>
              <a-descriptions-item label="手机号">{{ candidate.phone }}</a-descriptions-item>
              <a-descriptions-item label="当前职位">{{ candidate.currentPosition }}</a-descriptions-item>
              <a-descriptions-item label="当前公司">{{ candidate.currentCompany }}</a-descriptions-item>
              <a-descriptions-item label="学历">{{ candidate.education || candidate.workExperience }}</a-descriptions-item>
              <a-descriptions-item label="期望薪资" :span="2">
                {{ candidate.expectedSalary ? `${(candidate.expectedSalary / 1000).toFixed(0)}K/月` : '-' }}
              </a-descriptions-item>
              <a-descriptions-item label="技能" :span="2">
                <a-tag v-for="skill in skills" :key="skill" color="blue">
                  {{ skill }}
                </a-tag>
                <span v-if="!skills || skills.length === 0">-</span>
              </a-descriptions-item>
              <a-descriptions-item label="工作经历" :span="2">
                {{ candidate.workExperience || '-' }}
              </a-descriptions-item>
            </a-descriptions>
          </a-card>

          <a-card title="关联简历" class="resume-card">
            <a-table
              :columns="resumeColumns"
              :dataSource="resumes"
              :pagination="false"
              rowKey="id"
              size="small"
            >
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'source'">
                  <a-tag :color="getSourceColor(record.source)">
                    {{ getSourceText(record.source) }}
                  </a-tag>
                </template>
                <template v-else-if="column.key === 'parsedStatus'">
                  <a-tag :color="getStatusColor(record.parsedStatus)">
                    {{ getStatusText(record.parsedStatus) }}
                  </a-tag>
                </template>
                <template v-else-if="column.key === 'action'">
                  <a-button type="link" size="small" @click="handleParseResume(record)" :loading="parsingId === record.id">
                    解析
                  </a-button>
                </template>
              </template>
            </a-table>
          </a-card>
        </a-col>

        <a-col :span="8">
          <a-card title="操作" class="action-card">
            <a-space direction="vertical" style="width: 100%">
              <a-button type="primary" block @click="handleMatch">智能匹配职位</a-button>
              <a-button block @click="handleExport">导出数据</a-button>
            </a-space>
          </a-card>
        </a-col>
      </a-row>
    </a-spin>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { resumeApi } from '@/api/resume'
import api from '@/api'

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const candidate = ref(null)
const resumes = ref([])
const parsingId = ref(null)

const skills = computed(() => {
  if (!candidate.value?.skills) return []
  try {
    return JSON.parse(candidate.value.skills)
  } catch {
    return []
  }
})

const resumeColumns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '来源', key: 'source', width: 100 },
  { title: '解析状态', key: 'parsedStatus', width: 100 },
  { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt', width: 160 },
  { title: '操作', key: 'action', width: 80 }
]

function getSourceColor(source) {
  const colors = {
    Boss: 'purple',
    Zhaopin: 'blue',
    Lagou: 'cyan',
    Liepin: 'magenta',
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
    Manual: '手动录入'
  }
  return texts[source] || source
}

function getStatusColor(status) {
  const colors = {
    Pending: 'default',
    Parsing: 'processing',
    Success: 'success',
    Failed: 'error'
  }
  return colors[status] || 'default'
}

function getStatusText(status) {
  const texts = {
    Pending: '待解析',
    Parsing: '解析中',
    Success: '已解析',
    Failed: '解析失败'
  }
  return texts[status] || status
}

async function fetchCandidate() {
  loading.value = true
  try {
    const res = await api.get(`/candidates/${route.params.id}`)
    candidate.value = res.data
  } catch (e) {
    console.error(e)
    message.error('获取候选人信息失败')
  } finally {
    loading.value = false
  }
}

async function fetchResumes() {
  try {
    const res = await api.get(`/candidates/${route.params.id}/resumes`)
    resumes.value = res.data || []
  } catch (e) {
    console.error(e)
  }
}

async function handleParseResume(record) {
  parsingId.value = record.id
  try {
    await resumeApi.parse(record.id)
    message.success('解析成功')
    await fetchResumes()
    await fetchCandidate()
  } catch (e) {
    console.error(e)
    message.error('解析失败')
  } finally {
    parsingId.value = null
  }
}

function handleMatch() {
  router.push(`/matches?candidateId=${route.params.id}`)
}

function handleExport() {
  window.open(`/api/candidates/${route.params.id}/export`, '_blank')
}

onMounted(() => {
  fetchCandidate()
  fetchResumes()
})
</script>

<style scoped>
.candidate-detail {
  padding: 24px;
}

.info-card {
  margin-bottom: 16px;
}

.resume-card {
  margin-bottom: 16px;
}

.action-card {
  position: sticky;
  top: 24px;
}
</style>
