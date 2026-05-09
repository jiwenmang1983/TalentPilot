<template>
  <div class="dashboard">
    <!-- Page Header -->
    <div class="page-header">
      <div class="page-header-left">
        <h1>📊 招聘数据看板</h1>
        <p>实时监控招聘指标与业务数据</p>
      </div>
    </div>

    <!-- KPI Cards -->
    <div class="kpi-grid">
      <div class="kpi-card">
        <div class="kpi-icon green">💼</div>
        <div class="kpi-label">总职位数</div>
        <div class="kpi-value">{{ kpiData.totalJobs }}</div>
        <div class="kpi-sub">全部发布职位</div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon blue">🎯</div>
        <div class="kpi-label">在招职位</div>
        <div class="kpi-value">{{ kpiData.activeJobs }}</div>
        <div class="kpi-sub">正在招聘中</div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon purple">📄</div>
        <div class="kpi-label">简历投递数</div>
        <div class="kpi-value">{{ kpiData.totalResumes }}</div>
        <div class="kpi-sub">累计投递</div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon orange">🎤</div>
        <div class="kpi-label">面试邀约数</div>
        <div class="kpi-value">{{ kpiData.interviewCount }}</div>
        <div class="kpi-sub">已发起邀约</div>
      </div>
    </div>

    <!-- Charts Row -->
    <div class="chart-row">
      <!-- Funnel Chart -->
      <div class="chart-card">
        <h3>📈 招聘漏斗</h3>
        <v-chart :option="funnelOption" style="height: 320px" autoresize />
      </div>
      <!-- Trend Line Chart -->
      <div class="chart-card">
        <h3>📉 月度趋势</h3>
        <v-chart :option="trendOption" style="height: 320px" autoresize />
      </div>
    </div>

    <!-- Recent Activities -->
    <div class="activity-list">
      <h3>🕐 最近动态</h3>
      <div
        v-for="activity in recentActivities"
        :key="activity.id"
        class="activity-item"
      >
        <div class="activity-dot">●</div>
        <div class="activity-text">{{ activity.text }}</div>
        <div class="activity-time">{{ activity.time }}</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart, LineChart } from 'echarts/charts'
import { GridComponent, TooltipComponent, LegendComponent } from 'echarts/components'
import VChart from 'vue-echarts'

use([CanvasRenderer, BarChart, LineChart, GridComponent, TooltipComponent, LegendComponent])

// KPI Data
const kpiData = ref({
  totalJobs: 28,
  activeJobs: 12,
  totalResumes: 156,
  interviewCount: 43
})

// Funnel Data
const funnelData = ref([
  { stage: '投递', count: 156 },
  { stage: '初筛', count: 89 },
  { stage: '面试', count: 43 },
  { stage: '录用', count: 8 }
])

// Trend Data
const trendData = ref({
  months: ['11月', '12月', '1月', '2月', '3月', '4月'],
  resumes: [18, 24, 31, 28, 35, 20],
  interviews: [5, 8, 12, 9, 14, 8]
})

// Recent Activities
const recentActivities = ref([
  { id: 1, text: '候选人 李四 通过初筛，进入面试环节', time: '10分钟前' },
  { id: 2, text: '职位 Java高级工程师 新增2份投递', time: '25分钟前' },
  { id: 3, text: '候选人 王五 被录用', time: '1小时前' },
  { id: 4, text: '职位 产品经理 完成发布', time: '2小时前' },
  { id: 5, text: '候选人 赵六 面试完成，等待评估', time: '3小时前' }
])

// Funnel Chart Option
const funnelOption = ref({
  tooltip: {
    trigger: 'item',
    formatter: '{b}: {c}人'
  },
  grid: {
    left: '3%',
    right: '4%',
    bottom: '3%',
    top: '10%',
    containLabel: true
  },
  xAxis: {
    type: 'category',
    data: funnelData.value.map(d => d.stage),
    axisLabel: {
      fontSize: 12,
      color: '#6B7280'
    },
    axisLine: {
      lineStyle: {
        color: '#E5E7EB'
      }
    }
  },
  yAxis: {
    type: 'value',
    axisLabel: {
      fontSize: 12,
      color: '#6B7280'
    },
    axisLine: {
      show: false
    },
    splitLine: {
      lineStyle: {
        color: '#F3F4F6'
      }
    }
  },
  series: [
    {
      name: '招聘漏斗',
      type: 'bar',
      data: funnelData.value.map((d, i) => ({
        value: d.count,
        itemStyle: {
          color: ['#10B981', '#3B82F6', '#8B5CF6', '#F59E0B'][i]
        }
      })),
      barWidth: '40%',
      label: {
        show: true,
        position: 'top',
        fontSize: 12,
        color: '#6B7280',
        formatter: '{c}人'
      },
      itemStyle: {
        borderRadius: [4, 4, 0, 0]
      }
    }
  ]
})

// Trend Line Chart Option
const trendOption = ref({
  tooltip: {
    trigger: 'axis',
    axisPointer: {
      type: 'cross'
    }
  },
  legend: {
    data: ['投递数', '面试数'],
    bottom: 0,
    textStyle: {
      fontSize: 12,
      color: '#6B7280'
    }
  },
  grid: {
    left: '3%',
    right: '4%',
    bottom: '15%',
    top: '5%',
    containLabel: true
  },
  xAxis: {
    type: 'category',
    data: trendData.value.months,
    boundaryGap: false,
    axisLabel: {
      fontSize: 12,
      color: '#6B7280'
    },
    axisLine: {
      lineStyle: {
        color: '#E5E7EB'
      }
    }
  },
  yAxis: {
    type: 'value',
    axisLabel: {
      fontSize: 12,
      color: '#6B7280'
    },
    axisLine: {
      show: false
    },
    splitLine: {
      lineStyle: {
        color: '#F3F4F6'
      }
    }
  },
  series: [
    {
      name: '投递数',
      type: 'line',
      smooth: true,
      data: trendData.value.resumes,
      lineStyle: {
        width: 2,
        color: '#3B82F6'
      },
      itemStyle: {
        color: '#3B82F6'
      },
      areaStyle: {
        color: {
          type: 'linear',
          x: 0,
          y: 0,
          x2: 0,
          y2: 1,
          colorStops: [
            { offset: 0, color: 'rgba(59, 130, 246, 0.3)' },
            { offset: 1, color: 'rgba(59, 130, 246, 0.05)' }
          ]
        }
      },
      symbol: 'circle',
      symbolSize: 6
    },
    {
      name: '面试数',
      type: 'line',
      smooth: true,
      data: trendData.value.interviews,
      lineStyle: {
        width: 2,
        color: '#10B981'
      },
      itemStyle: {
        color: '#10B981'
      },
      areaStyle: {
        color: {
          type: 'linear',
          x: 0,
          y: 0,
          x2: 0,
          y2: 1,
          colorStops: [
            { offset: 0, color: 'rgba(16, 185, 129, 0.3)' },
            { offset: 1, color: 'rgba(16, 185, 129, 0.05)' }
          ]
        }
      },
      symbol: 'circle',
      symbolSize: 6
    }
  ]
})
</script>

<style scoped>
.dashboard {
  width: 100%;
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

.kpi-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 24px;
}

.kpi-card {
  background: #fff;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
  border: 1px solid #E5E7EB;
  transition: box-shadow 0.15s;
}

.kpi-card:hover {
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}

.kpi-icon {
  font-size: 24px;
  margin-bottom: 12px;
}

.kpi-icon.green { color: #10B981; }
.kpi-icon.blue { color: #3B82F6; }
.kpi-icon.purple { color: #8B5CF6; }
.kpi-icon.orange { color: #F59E0B; }

.kpi-label {
  font-size: 12px;
  color: #6B7280;
  margin-bottom: 4px;
}

.kpi-value {
  font-size: 28px;
  font-weight: 700;
  color: #1F2937;
  margin-bottom: 4px;
}

.kpi-sub {
  font-size: 12px;
  color: #9CA3AF;
}

.chart-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  margin-bottom: 24px;
}

.chart-card {
  background: #fff;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
  border: 1px solid #E5E7EB;
}

.chart-card h3 {
  font-size: 14px;
  font-weight: 600;
  color: #1F2937;
  margin: 0 0 16px;
}

.activity-list {
  background: #fff;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
  border: 1px solid #E5E7EB;
}

.activity-list h3 {
  font-size: 14px;
  font-weight: 600;
  color: #1F2937;
  margin: 0 0 16px;
}

.activity-item {
  display: flex;
  gap: 12px;
  padding: 10px 0;
  border-bottom: 1px solid #F3F4F6;
  font-size: 13px;
}

.activity-item:last-child {
  border-bottom: none;
}

.activity-dot {
  color: #0D3D92;
  font-size: 8px;
  margin-top: 4px;
}

.activity-text {
  flex: 1;
  color: #374151;
}

.activity-time {
  color: #9CA3AF;
  white-space: nowrap;
}
</style>
