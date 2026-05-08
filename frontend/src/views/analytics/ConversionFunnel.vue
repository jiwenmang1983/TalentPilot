<template>
  <div class="conversion-funnel">
    <!-- Summary Cards -->
    <a-row :gutter="16" class="summary-cards">
      <a-col :span="6">
        <a-card>
          <a-statistic title="总发布职位数" :value="summary.totalJobs" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card>
          <a-statistic title="总投递数" :value="summary.totalApplications" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card>
          <a-statistic title="平均转化率" :value="summary.avgConversionRate" suffix="%" :precision="1" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card>
          <a-statistic title="入职人数" :value="summary.totalHired" />
        </a-card>
      </a-col>
    </a-row>

    <!-- Filter and Actions -->
    <div class="toolbar">
      <a-space wrap>
        <a-range-picker
          v-model:value="dateRange"
          @change="handleSearch"
          style="width: 250px"
        />
        <a-select
          v-model:value="selectedJobPostId"
          placeholder="选择职位"
          style="width: 200px"
          allow-clear
          @change="handleSearch"
        >
          <a-select-option v-for="job in jobPosts" :key="job.id" :value="job.id">
            {{ job.title }}
          </a-select-option>
        </a-select>
        <a-button type="primary" @click="handleSearch">
          <template #icon><SearchOutlined /></template>
          搜索
        </a-button>
        <a-button @click="handleSeedData" :loading="seeding">
          生成演示数据
        </a-button>
      </a-space>
    </div>

    <!-- Funnel Chart -->
    <a-card title="招聘漏斗图" class="chart-card">
      <div ref="funnelChartRef" style="width: 100%; height: 400px"></div>
    </a-card>

    <!-- Data Table -->
    <a-card title="详细数据" class="table-card">
      <a-table
        :columns="columns"
        :data-source="funnelData"
        :loading="loading"
        :pagination="pagination"
        @change="handleTableChange"
        row-key="id"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'conversionRate'">
            <a-progress :percent="record.conversionRate" :format="(p) => p + '%'" size="small" style="width: 100px" />
          </template>
          <template v-else-if="column.key === 'period'">
            {{ formatDate(record.periodStart) }} ~ {{ formatDate(record.periodEnd) }}
          </template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onUnmounted, watch, nextTick } from 'vue'
import { message } from 'ant-design-vue'
import { SearchOutlined } from '@ant-design/icons-vue'
import { conversionFunnelApi } from '@/api/conversionFunnel'
import { jobPostApi } from '@/api/jobpost'
import * as echarts from 'echarts'
import dayjs from 'dayjs'

const funnelChartRef = ref(null)
let funnelChart = null

const funnelData = ref([])
const jobPosts = ref([])
const loading = ref(false)
const seeding = ref(false)

const selectedJobPostId = ref(null)
const dateRange = ref([dayjs().subtract(30, 'day'), dayjs()])

const summary = reactive({
  totalJobs: 0,
  totalApplications: 0,
  avgConversionRate: 0,
  totalHired: 0
})

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
  showSizeChanger: true,
  showTotal: (total) => `共 ${total} 条`
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 60 },
  { title: '职位', dataIndex: 'jobPostTitle', key: 'jobPostTitle' },
  { title: '阶段', dataIndex: 'stage', key: 'stage', width: 100 },
  { title: '人数', dataIndex: 'count', key: 'count', width: 80 },
  { title: '转化率', key: 'conversionRate', width: 150 },
  { title: '平均停留(天)', dataIndex: 'avgTimeSpent', key: 'avgTimeSpent', width: 120 },
  { title: '统计周期', key: 'period', width: 220 }
]

function formatDate(date) {
  return dayjs(date).format('YYYY-MM-DD')
}

async function fetchJobPosts() {
  try {
    const res = await jobPostApi.list({ page: 1, pageSize: 100 })
    jobPosts.value = res.data?.items || res.items || []
  } catch (e) {
    console.error('Failed to fetch job posts:', e)
  }
}

async function fetchFunnelData() {
  loading.value = true
  try {
    const params = {
      page: pagination.current,
      pageSize: pagination.pageSize,
      jobPostId: selectedJobPostId.value || undefined,
      dateFrom: dateRange.value?.[0]?.toISOString(),
      dateTo: dateRange.value?.[1]?.toISOString()
    }
    const res = await conversionFunnelApi.list(params)
    funnelData.value = res.data?.items || res.items || []
    pagination.total = res.data?.total || res.total || 0
  } catch (e) {
    message.error('获取漏斗数据失败')
  } finally {
    loading.value = false
  }
}

async function fetchSummary() {
  try {
    const params = {
      dateFrom: dateRange.value?.[0]?.toISOString(),
      dateTo: dateRange.value?.[1]?.toISOString()
    }
    const res = await conversionFunnelApi.summary(params)
    const data = res.data || res || []

    // Calculate summary from stage data
    const stages = ['Posted', 'Applied', 'Matched', 'Interviewed', 'Hired']
    const stageData = {}
    stages.forEach(s => {
      const item = data.find(d => d.stage === s)
      stageData[s] = item || { totalCount: 0, avgConversionRate: 0, avgDays: 0 }
    })

    summary.totalJobs = selectedJobPostId.value ? 1 : (jobPosts.value.length || 0)
    summary.totalApplications = stageData.Applied.totalCount
    summary.avgConversionRate = stageData.Applied.avgConversionRate
    summary.totalHired = stageData.Hired.totalCount
  } catch (e) {
    console.error('Failed to fetch summary:', e)
  }
}

async function fetchChartData() {
  try {
    const params = {
      jobPostId: selectedJobPostId.value || undefined,
      dateFrom: dateRange.value?.[0]?.toISOString(),
      dateTo: dateRange.value?.[1]?.toISOString()
    }
    const res = await conversionFunnelApi.chart(params)
    const data = res.data || res

    updateFunnelChart(data.stages || [], data.counts || [], data.rates || [])
  } catch (e) {
    console.error('Failed to fetch chart data:', e)
  }
}

function updateFunnelChart(stages, counts, rates) {
  if (!funnelChart) {
    funnelChart = echarts.init(funnelChartRef.value)
  }

  const option = {
    tooltip: {
      trigger: 'item',
      formatter: '{b}: {c}人 ({d}%)'
    },
    legend: {
      data: stages,
      bottom: 10
    },
    series: [
      {
        name: '招聘漏斗',
        type: 'funnel',
        left: '10%',
        top: 60,
        bottom: 60,
        width: '80%',
        min: 0,
        max: 100,
        minSize: '0%',
        maxSize: '100%',
        sort: 'descending',
        gap: 2,
        label: {
          show: true,
          position: 'inside',
          formatter: '{b}\n{c}人'
        },
        labelLayout: {
          moveToGap: true
        },
        itemStyle: {
          borderColor: '#fff',
          borderWidth: 1
        },
        emphasis: {
          label: {
            fontSize: 16
          }
        },
        data: stages.map((stage, i) => ({
          value: counts[i] || 0,
          name: stage,
          itemStyle: {
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: getStageColor(i) },
              { offset: 1, color: getStageColor(i, true) }
            ])
          }
        }))
      }
    ]
  }

  funnelChart.setOption(option, true)
}

function getStageColor(index, darker = false) {
  const colors = [
    ['#1890ff', '#096dd9'],
    ['#52c41a', '#389e0d'],
    ['#faad14', '#d48806'],
    ['#fa8c16', '#d46b08'],
    ['#f5222d', '#cf1322']
  ]
  const colorSet = colors[index % colors.length]
  return darker ? colorSet[1] : colorSet[0]
}

async function handleSeedData() {
  seeding.value = true
  try {
    await conversionFunnelApi.seed()
    message.success('演示数据生成成功')
    handleSearch()
  } catch (e) {
    message.error('生成失败')
  } finally {
    seeding.value = false
  }
}

function handleSearch() {
  pagination.current = 1
  fetchFunnelData()
  fetchSummary()
  fetchChartData()
}

function handleTableChange(pag) {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  fetchFunnelData()
}

function handleResize() {
  funnelChart?.resize()
}

onMounted(async () => {
  await fetchJobPosts()
  await fetchFunnelData()
  await fetchSummary()
  await fetchChartData()

  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
  funnelChart?.dispose()
})

watch(dateRange, () => {
  handleSearch()
})
</script>

<style scoped>
.conversion-funnel {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.summary-cards {
  margin-bottom: 16px;
}

.toolbar {
  margin-bottom: 16px;
}

.chart-card {
  margin-bottom: 16px;
}

.table-card {
  margin-bottom: 16px;
}
</style>