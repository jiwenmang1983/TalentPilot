<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>📋 操作日志</h1>
      <p>系统操作记录查询</p>
    </div>
  </div>

  <div class="operation-logs">
    <div class="toolbar">
      <a-space wrap>
        <a-range-picker
          v-model:value="dateRange"
          :placeholder="['开始日期', '结束日期']"
          @change="handleSearch"
        />
        <a-select
          v-model:value="filterUser"
          placeholder="操作用户"
          style="width: 150px"
          allow-clear
          show-search
          :filter-option="filterOption"
          @change="handleSearch"
        >
          <a-select-option v-for="user in users" :key="user.id" :value="user.id">
            {{ user.username }}
          </a-select-option>
        </a-select>
        <a-select
          v-model:value="filterAction"
          placeholder="操作类型"
          style="width: 150px"
          allow-clear
          @change="handleSearch"
        >
          <a-select-option value="Create">创建</a-select-option>
          <a-select-option value="Update">更新</a-select-option>
          <a-select-option value="Delete">删除</a-select-option>
          <a-select-option value="Login">登录</a-select-option>
          <a-select-option value="Logout">登出</a-select-option>
        </a-select>
        <a-button type="primary" @click="handleSearch">
          <template #icon><SearchOutlined /></template>
          搜索
        </a-button>
      </a-space>
    </div>

    <a-table
      :columns="columns"
      :data-source="logs"
      :loading="loading"
      :pagination="pagination"
      @change="handleTableChange"
      row-key="id"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'action'">
          <a-tag :color="getActionColor(record.action)">
            {{ getActionText(record.action) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'success'">
          <a-tag :color="record.success ? 'green' : 'red'">
            {{ record.success ? '成功' : '失败' }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'actions'">
          <a-button type="link" size="small" @click="showDetail(record)">
            详情
          </a-button>
        </template>
      </template>
    </a-table>

    <a-modal
      v-model:open="detailVisible"
      title="日志详情"
      :footer="null"
      width="600px"
    >
      <a-descriptions :column="2" bordered v-if="currentLog">
        <a-descriptions-item label="ID">{{ currentLog.id }}</a-descriptions-item>
        <a-descriptions-item label="操作用户">{{ currentLog.user?.username || '-' }}</a-descriptions-item>
        <a-descriptions-item label="操作类型">{{ getActionText(currentLog.action) }}</a-descriptions-item>
        <a-descriptions-item label="状态">
          <a-tag :color="currentLog.success ? 'green' : 'red'">
            {{ currentLog.success ? '成功' : '失败' }}
          </a-tag>
        </a-descriptions-item>
        <a-descriptions-item label="IP地址">{{ currentLog.ipAddress || '-' }}</a-descriptions-item>
        <a-descriptions-item label="用户代理">{{ currentLog.userAgent || '-' }}</a-descriptions-item>
        <a-descriptions-item label="操作时间" :span="2">{{ currentLog.createdAt }}</a-descriptions-item>
        <a-descriptions-item label="资源类型">{{ currentLog.resourceType || '-' }}</a-descriptions-item>
        <a-descriptions-item label="资源ID">{{ currentLog.resourceId || '-' }}</a-descriptions-item>
        <a-descriptions-item label="操作描述" :span="2">{{ currentLog.description || '-' }}</a-descriptions-item>
        <a-descriptions-item label="请求数据" :span="2">
          <pre v-if="currentLog.requestData" class="json-data">{{ JSON.stringify(JSON.parse(currentLog.requestData), null, 2) }}</pre>
          <span v-else>-</span>
        </a-descriptions-item>
        <a-descriptions-item label="响应数据" :span="2">
          <pre v-if="currentLog.responseData" class="json-data">{{ JSON.stringify(JSON.parse(currentLog.responseData), null, 2) }}</pre>
          <span v-else>-</span>
        </a-descriptions-item>
        <a-descriptions-item v-if="currentLog.errorMessage" label="错误信息" :span="2">
          <span style="color: #ff4d4f">{{ currentLog.errorMessage }}</span>
        </a-descriptions-item>
      </a-descriptions>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { SearchOutlined } from '@ant-design/icons-vue'
import { operationLogApi, userApi } from '@/api'
import dayjs from 'dayjs'

const logs = ref([])
const users = ref([])
const loading = ref(false)
const detailVisible = ref(false)
const currentLog = ref(null)

const dateRange = ref(null)
const filterUser = ref(null)
const filterAction = ref(null)

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
  showSizeChanger: true,
  showTotal: (total) => `共 ${total} 条`
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 80 },
  { title: '操作用户', dataIndex: ['user', 'username'], key: 'user' },
  { title: '操作类型', key: 'action', width: 100 },
  { title: '资源类型', dataIndex: 'resourceType', key: 'resourceType' },
  { title: '资源ID', dataIndex: 'resourceId', key: 'resourceId' },
  { title: '描述', dataIndex: 'description', key: 'description', ellipsis: true },
  { title: '状态', key: 'success', width: 80 },
  { title: 'IP地址', dataIndex: 'ipAddress', key: 'ipAddress', width: 130 },
  { title: '操作时间', dataIndex: 'createdAt', key: 'createdAt', width: 180 },
  { title: '操作', key: 'actions', width: 80, fixed: 'right' }
]

function getActionColor(action) {
  const colors = {
    Create: 'green',
    Update: 'blue',
    Delete: 'red',
    Login: 'cyan',
    Logout: 'default'
  }
  return colors[action] || 'default'
}

function getActionText(action) {
  const texts = {
    Create: '创建',
    Update: '更新',
    Delete: '删除',
    Login: '登录',
    Logout: '登出'
  }
  return texts[action] || action
}

function filterOption(input, option) {
  return option.children.props.children.toLowerCase().includes(input.toLowerCase())
}

async function fetchLogs() {
  loading.value = true
  try {
    const params = {
      page: pagination.current,
      pageSize: pagination.pageSize,
      startDate: dateRange.value?.[0]?.format('YYYY-MM-DD') || undefined,
      endDate: dateRange.value?.[1]?.format('YYYY-MM-DD') || undefined,
      userId: filterUser.value || undefined,
      action: filterAction.value || undefined
    }
    const res = await operationLogApi.list(params)
    logs.value = res.data?.items || res.items || []
    pagination.total = res.data?.total || res.total || 0
  } catch (e) {
    message.error('获取日志列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchUsers() {
  try {
    const res = await userApi.list({ pageSize: 100 })
    users.value = res.data?.items || res.items || []
  } catch (e) {
    console.error('Failed to fetch users:', e)
  }
}

function handleSearch() {
  pagination.current = 1
  fetchLogs()
}

function handleTableChange(pag) {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  fetchLogs()
}

function showDetail(record) {
  currentLog.value = record
  detailVisible.value = true
}

onMounted(() => {
  fetchLogs()
  fetchUsers()
})
</script>

<style scoped>
.operation-logs {
  height: 100%;
}

.toolbar {
  margin-bottom: 16px;
}

.json-data {
  background: #f5f5f5;
  padding: 12px;
  border-radius: 4px;
  font-size: 12px;
  max-height: 200px;
  overflow: auto;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>