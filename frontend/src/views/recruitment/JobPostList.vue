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
      <a-space>
        <a-button @click="openAdaptDrawer">
          <template #icon><AimOutlined /></template>
          AI内容适配
        </a-button>
        <a-button type="primary" @click="$router.push('/jobposts/new')">
          <template #icon><PlusOutlined /></template>
          新建职位
        </a-button>
      </a-space>
    </div>

    <a-table
      :columns="columns"
      :dataSource="dataSource"
      :loading="loading"
      :pagination="pagination"
      :row-selection="rowSelection"
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

    <!-- Content Adaptation Drawer -->
    <a-drawer
      v-model:open="drawerVisible"
      :title="`职位内容适配 - ${selectedJobPost?.title || ''}`"
      width="700"
      @close="closeDrawer"
    >
      <template v-if="!adaptStarted">
        <a-space direction="vertical" style="width: 100%" :size="16">
          <a-alert type="info" show-icon>
            <template #message>选择要适配的渠道</template>
            <template #description>系统将根据职位信息为各渠道生成定制化的职位描述内容</template>
          </a-alert>
          <a-checkbox-group v-model:value="selectedChannels" :options="channelOptions" />
          <a-button type="primary" :disabled="selectedChannels.length === 0" :loading="adapting" @click="startAdapt">
            开始适配
          </a-button>
        </a-space>
      </template>

      <template v-else>
        <div class="adapt-content">
          <div class="adapt-toolbar">
            <a-button @click="loadContents" :loading="loadingContents">
              <template #icon><ReloadOutlined /></template>
              刷新
            </a-button>
          </div>

          <a-spin :spinning="loadingContents">
            <a-table
              :columns="contentColumns"
              :dataSource="adaptedContents"
              :pagination="false"
              rowKey="id"
              size="small"
            >
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'channelType'">
                  {{ getChannelName(record.channelType) }}
                </template>
                <template v-else-if="column.key === 'status'">
                  <a-badge :status="getContentStatusBadge(record.status)" :text="getContentStatusText(record.status)" />
                </template>
                <template v-else-if="column.key === 'wordCount'">
                  {{ (record.adaptedContent || '').length }}
                </template>
                <template v-else-if="column.key === 'action'">
                  <a-space>
                    <a @click="openPreview(record)">预览</a>
                    <a-divider type="vertical" />
                    <a @click="openEdit(record)">编辑</a>
                  </a-space>
                </template>
              </template>
            </a-table>
          </a-spin>
        </div>
      </template>
    </a-drawer>

    <!-- Preview Modal -->
    <a-modal v-model:open="previewVisible" title="内容预览" :footer="null" width="800">
      <a-divider orientation="left">渠道名称</a-divider>
      <p>{{ previewRecord?.channelType }}</p>
      <a-divider orientation="left">适配标题</a-divider>
      <p>{{ previewRecord?.adaptedTitle }}</p>
      <a-divider orientation="left">适配内容</a-divider>
      <div style="white-space: pre-wrap; max-height: 400px; overflow-y: auto;">
        {{ previewRecord?.adaptedContent }}
      </div>
    </a-modal>

    <!-- Edit Modal -->
    <a-modal
      v-model:open="editVisible"
      title="编辑内容"
      @ok="saveEdit"
      :confirmLoading="savingEdit"
      @cancel="editVisible = false"
    >
      <a-space direction="vertical" style="width: 100%" :size="16">
        <div>
          <label>适配标题</label>
          <a-input v-model:value="editForm.adaptedTitle" style="margin-top: 8px" />
        </div>
        <div>
          <label>适配内容</label>
          <a-textarea v-model:value="editForm.adaptedContent" :rows="10" style="margin-top: 8px" />
        </div>
      </a-space>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { PlusOutlined, AimOutlined, ReloadOutlined } from '@ant-design/icons-vue'
import { jobPostApi } from '@/api/jobpost'
import { jobChannelContentApi } from '@/api/jobChannelContent'
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
const selectedRowKeys = ref([])
const selectedJobPost = ref(null)

const drawerVisible = ref(false)
const adaptStarted = ref(false)
const adapting = ref(false)
const selectedChannels = ref(['Liepin', 'Lagou', 'Boss', 'Linkedin', 'Xiaohongshu', 'Custom'])
const channelOptions = [
  { label: '猎聘', value: 'Liepin' },
  { label: '拉勾', value: 'Lagou' },
  { label: 'Boss直聘', value: 'Boss' },
  { label: '领英', value: 'Linkedin' },
  { label: '小红书', value: 'Xiaohongshu' },
  { label: '自定义', value: 'Custom' }
]

const loadingContents = ref(false)
const adaptedContents = ref([])
const contentColumns = [
  { title: '渠道', key: 'channelType', width: 120 },
  { title: '状态', key: 'status', width: 100 },
  { title: '字数', key: 'wordCount', width: 80 },
  { title: '操作', key: 'action', width: 120 }
]

const previewVisible = ref(false)
const previewRecord = ref(null)

const editVisible = ref(false)
const editRecord = ref(null)
const editForm = ref({ adaptedTitle: '', adaptedContent: '' })
const savingEdit = ref(false)

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

const rowSelection = {
  selectedRowKeys,
  onChange: (keys, records) => {
    selectedRowKeys.value = keys
    selectedJobPost.value = records[0] || null
  }
}

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

function getChannelName(channelType) {
  const names = {
    Liepin: '猎聘',
    Lagou: '拉勾',
    Boss: 'Boss直聘',
    Linkedin: '领英',
    Xiaohongshu: '小红书',
    Custom: '自定义'
  }
  return names[channelType] || channelType
}

function getContentStatusBadge(status) {
  const badges = { pending: 'warning', ready: 'success', failed: 'error' }
  return badges[status] || 'default'
}

function getContentStatusText(status) {
  const texts = { pending: '处理中', ready: '就绪', failed: '失败' }
  return texts[status] || status
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

function openAdaptDrawer() {
  if (!selectedJobPost.value) {
    message.warning('请先选择要适配的职位')
    return
  }
  drawerVisible.value = true
  adaptStarted.value = false
  selectedChannels.value = ['Liepin', 'Lagou', 'Boss', 'Linkedin', 'Xiaohongshu', 'Custom']
}

function closeDrawer() {
  drawerVisible.value = false
  selectedJobPost.value = null
  selectedRowKeys.value = []
  adaptStarted.value = false
  adaptedContents.value = []
}

async function startAdapt() {
  if (!selectedJobPost.value || selectedChannels.value.length === 0) return
  adapting.value = true
  try {
    await jobChannelContentApi.adapt(selectedJobPost.value.id, selectedChannels.value)
    message.success('已开始适配，请稍后刷新查看结果')
    adaptStarted.value = true
    await loadContents()
  } catch (e) {
    console.error(e)
    message.error('适配失败')
  } finally {
    adapting.value = false
  }
}

async function loadContents() {
  if (!selectedJobPost.value) return
  loadingContents.value = true
  try {
    const res = await jobChannelContentApi.getByJobPost(selectedJobPost.value.id)
    adaptedContents.value = res.data || []
  } catch (e) {
    console.error(e)
  } finally {
    loadingContents.value = false
  }
}

function openPreview(record) {
  previewRecord.value = record
  previewVisible.value = true
}

function openEdit(record) {
  editRecord.value = record
  editForm.value = {
    adaptedTitle: record.adaptedTitle || '',
    adaptedContent: record.adaptedContent || ''
  }
  editVisible.value = true
}

async function saveEdit() {
  if (!editRecord.value) return
  savingEdit.value = true
  try {
    await jobChannelContentApi.update(editRecord.value.id, {
      adaptedTitle: editForm.value.adaptedTitle,
      adaptedContent: editForm.value.adaptedContent
    })
    message.success('保存成功')
    editVisible.value = false
    await loadContents()
  } catch (e) {
    console.error(e)
  } finally {
    savingEdit.value = false
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

.adapt-content {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.adapt-toolbar {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 8px;
}
</style>
