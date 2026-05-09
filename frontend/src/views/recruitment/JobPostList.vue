<template>
  <div class="job-post-list">
    <!-- Page Header -->
    <div class="page-header">
      <div class="page-header-left">
        <h1>💼 职位管理</h1>
        <p>职位发布、编辑与全流程管理</p>
      </div>
    </div>

    <div class="toolbar">
      <a-space wrap>
        <a-select
          v-model:value="filterStatus"
          placeholder="筛选状态"
          style="width: 150px"
          allowClear
          @change="handleSearch"
        >
          <a-select-option value="Draft">草稿</a-select-option>
          <a-select-option value="Published">已发布</a-select-option>
          <a-select-option value="Paused">暂停</a-select-option>
          <a-select-option value="Closed">已关闭</a-select-option>
        </a-select>
      </a-space>
      <a-button type="primary" @click="$router.push('/jobposts/new')">
        <template #icon><PlusOutlined /></template>
        新建职位
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
        <template v-else-if="column.key === 'salary'">
          {{ formatSalary(record.salaryMin, record.salaryMax) }}
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a @click="handleEdit(record)">编辑</a>
            <a-divider type="vertical" />
            <a-dropdown>
              <a>更多</a>
              <template #overlay>
                <a-menu>
                  <a-menu-item v-if="record.status === 'Draft'" @click="handleUpdateStatus(record.id, 'Published')">
                    发布
                  </a-menu-item>
                  <a-menu-item v-if="record.status === 'Published'" @click="handleUpdateStatus(record.id, 'Paused')">
                    暂停
                  </a-menu-item>
                  <a-menu-item v-if="record.status === 'Paused'" @click="handleUpdateStatus(record.id, 'Published')">
                    恢复发布
                  </a-menu-item>
                  <a-menu-item v-if="record.status !== 'Closed'" @click="handleUpdateStatus(record.id, 'Closed')">
                    关闭
                  </a-menu-item>
                  <a-menu-divider />
                  <a-menu-item @click="handleDelete(record.id)" style="color: #ff4d4f">
                    删除
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
import { PlusOutlined } from '@ant-design/icons-vue'
import { jobPostApi } from '@/api/jobpost'
import { message } from 'ant-design-vue'

const router = useRouter()
const loading = ref(false)
const filterStatus = ref(null)
const dataSource = ref([])
const pagination = ref({
  current: 1,
  pageSize: 20,
  total: 0
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '职位名称', dataIndex: 'title', key: 'title' },
  { title: '部门', dataIndex: 'department', key: 'department' },
  { title: '薪资范围', key: 'salary', width: 180 },
  { title: '经验要求', dataIndex: 'experience', key: 'experience', width: 100 },
  { title: '学历要求', dataIndex: 'education', key: 'education', width: 100 },
  { title: '状态', key: 'status', width: 100 },
  { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt', width: 180 },
  { title: '操作', key: 'action', width: 150, fixed: 'right' }
]

function getStatusColor(status) {
  const colors = {
    Draft: 'default',
    Published: 'success',
    Paused: 'warning',
    Closed: 'error'
  }
  return colors[status] || 'default'
}

function getStatusText(status) {
  const texts = {
    Draft: '草稿',
    Published: '已发布',
    Paused: '暂停',
    Closed: '已关闭'
  }
  return texts[status] || status
}

function formatSalary(min, max) {
  if (!min && !max) return '面议'
  const format = (v) => (v ? `${(v / 10000).toFixed(1)}万` : '')
  if (min && max) return `${format(min)}-${format(max)}`
  return min ? `${format(min)}以上` : `${format(max)}以下`
}

async function fetchData() {
  loading.value = true
  try {
    const res = await jobPostApi.list({
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

function handleSearch() {
  pagination.value.current = 1
  fetchData()
}

function handleTableChange(pag) {
  pagination.value.current = pag.current
  pagination.value.pageSize = pag.pageSize
  fetchData()
}

function handleEdit(record) {
  router.push(`/jobposts/${record.id}`)
}

async function handleUpdateStatus(id, status) {
  try {
    await jobPostApi.updateStatus(id, status)
    message.success('状态更新成功')
    fetchData()
  } catch (e) {
    console.error(e)
  }
}

async function handleDelete(id) {
  try {
    await jobPostApi.delete(id)
    message.success('删除成功')
    fetchData()
  } catch (e) {
    console.error(e)
  }
}

onMounted(() => {
  fetchData()
})
</script>

<style scoped>
.job-post-list {
  padding: 0;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  padding: 0 4px;
}

.page-header h1 {
  font-size: 20px;
  font-weight: 600;
  color: #1F2937;
  margin: 0 0 4px;
}

.page-header p {
  font-size: 13px;
  color: #6B7280;
  margin: 0;
}

.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  background: #fff;
  padding: 12px 16px;
  border-radius: 8px;
  border: 1px solid #E5E7EB;
}
</style>
