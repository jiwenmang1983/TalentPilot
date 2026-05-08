<template>
  <div class="candidate-list">
    <div class="header-actions">
      <a-space>
        <a-input-search
          v-model:value="keyword"
          placeholder="搜索姓名/邮箱"
          style="width: 200px"
          @search="handleSearch"
        />
        <a-input-search
          v-model:value="phone"
          placeholder="搜索手机号"
          style="width: 150px"
          @search="handleSearch"
        />
      </a-space>
      <a-button type="primary" @click="handleCreate">新建候选人</a-button>
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
        <template v-if="column.key === 'skills'">
          <a-tag v-for="skill in parseSkills(record.skills)" :key="skill" color="blue">
            {{ skill }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'expectedSalary'">
          {{ formatSalary(record.expectedSalary) }}
        </template>
        <template v-else-if="column.key === 'action'">
          <a @click="handleView(record)">查看详情</a>
        </template>
      </template>
    </a-table>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'

const router = useRouter()
const loading = ref(false)
const keyword = ref('')
const phone = ref('')
const dataSource = ref([])
const pagination = ref({
  current: 1,
  pageSize: 20,
  total: 0
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '姓名', dataIndex: 'name', key: 'name' },
  { title: '邮箱', dataIndex: 'email', key: 'email' },
  { title: '手机号', dataIndex: 'phone', key: 'phone' },
  { title: '当前职位', dataIndex: 'currentPosition', key: 'currentPosition' },
  { title: '当前公司', dataIndex: 'currentCompany', key: 'currentCompany' },
  { title: '技能', key: 'skills', width: 250 },
  { title: '期望薪资', key: 'expectedSalary', width: 100 },
  { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt', width: 180 },
  { title: '操作', key: 'action', width: 100, fixed: 'right' }
]

function parseSkills(skills) {
  if (!skills) return []
  try {
    return JSON.parse(skills)
  } catch {
    return []
  }
}

function formatSalary(salary) {
  if (!salary) return '-'
  return `${(salary / 1000).toFixed(0)}K`
}

async function fetchData() {
  loading.value = true
  try {
    const res = await api.get('/candidates', {
      params: {
        keyword: keyword.value,
        phone: phone.value,
        page: pagination.value.current,
        pageSize: pagination.value.pageSize
      }
    })
    dataSource.value = res.data?.items || []
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

function handleTableChange(pag) {
  pagination.value.current = pag.current
  pagination.value.pageSize = pag.pageSize
  fetchData()
}

function handleView(record) {
  router.push(`/candidates/${record.id}`)
}

function handleCreate() {
  router.push('/candidates/new')
}

onMounted(() => {
  fetchData()
})

import api from '@/api'
</script>

<style scoped>
.candidate-list {
  padding: 24px;
}

.header-actions {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}
</style>
