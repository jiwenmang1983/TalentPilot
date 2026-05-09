<template>
  <!-- Page Header -->
  <div class="page-header">
    <div class="page-header-left">
      <h1>📄 简历管理</h1>
      <p>候选人简历采集与管理</p>
    </div>
  </div>

  <div class="resume-list">
    <div class="header-actions">
      <a-space>
        <a-select
          v-model:value="filterSource"
          placeholder="筛选来源"
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
        <a-button @click="handleMockCollect">模拟采集</a-button>
        <a-button type="primary" @click="handleUpload">上传简历</a-button>
      </a-space>
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
      </template>
    </a-table>

    <a-modal v-model:open="uploadVisible" title="上传简历" @ok="handleUploadSubmit" :confirmLoading="uploading">
      <a-form :model="uploadForm" layout="vertical">
        <a-form-item label="姓名" name="candidateName">
          <a-input v-model:value="uploadForm.candidateName" placeholder="请输入候选人姓名" />
        </a-form-item>
        <a-form-item label="手机号" name="phone">
          <a-input v-model:value="uploadForm.phone" placeholder="请输入手机号" />
        </a-form-item>
        <a-form-item label="邮箱" name="email">
          <a-input v-model:value="uploadForm.email" placeholder="请输入邮箱" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal v-model:open="collectVisible" title="模拟采集" @ok="handleCollectSubmit" :confirmLoading="collecting">
      <a-form :model="collectForm" layout="vertical">
        <a-form-item label="渠道" name="channel">
          <a-select v-model:value="collectForm.channel" placeholder="请选择渠道">
            <a-select-option value="Boss">Boss直聘</a-select-option>
            <a-select-option value="Zhaopin">智联招聘</a-select-option>
            <a-select-option value="Lagou">拉勾网</a-select-option>
            <a-select-option value="Liepin">猎聘网</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="采集数量" name="count">
          <a-input-number v-model:value="collectForm.count" :min="1" :max="20" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { resumeApi } from '@/api/resume'

const loading = ref(false)
const filterSource = ref(null)
const dataSource = ref([])
const pagination = ref({
  current: 1,
  pageSize: 20,
  total: 0
})

const uploadVisible = ref(false)
const uploading = ref(false)
const uploadForm = reactive({
  candidateName: '',
  phone: '',
  email: ''
})

const collectVisible = ref(false)
const collecting = ref(false)
const collectForm = reactive({
  channel: 'Boss',
  count: 5
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '姓名', dataIndex: 'candidateName', key: 'candidateName' },
  { title: '手机号', dataIndex: 'phone', key: 'phone' },
  { title: '邮箱', dataIndex: 'email', key: 'email' },
  { title: '来源', key: 'source', width: 120 },
  { title: '解析状态', key: 'parsedStatus', width: 100 },
  { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt', width: 180 }
]

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

async function fetchData() {
  loading.value = true
  try {
    const res = await resumeApi.list({
      source: filterSource.value,
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

function handleUpload() {
  uploadVisible.value = true
  Object.assign(uploadForm, { candidateName: '', phone: '', email: '' })
}

async function handleUploadSubmit() {
  try {
    uploading.value = true
    await resumeApi.upload(uploadForm)
    message.success('上传成功')
    uploadVisible.value = false
    fetchData()
  } catch (e) {
    console.error(e)
  } finally {
    uploading.value = false
  }
}

function handleMockCollect() {
  collectVisible.value = true
}

async function handleCollectSubmit() {
  try {
    collecting.value = true
    const res = await resumeApi.mockCollect(collectForm)
    message.success(`模拟采集成功，共采集${res.data?.count || 0}份简历`)
    collectVisible.value = false
    fetchData()
  } catch (e) {
    console.error(e)
  } finally {
    collecting.value = false
  }
}

onMounted(() => {
  fetchData()
})
</script>

<style scoped>
.resume-list {
  padding: 24px;
}

.header-actions {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}
</style>
