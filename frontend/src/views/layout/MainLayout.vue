<template>
  <a-layout class="app-layout">
    <!-- Sider -->
    <a-layout-sider
      v-model:collapsed="collapsed"
      :trigger="null"
      collapsible
      :width="240"
      :collapsed-width="64"
      class="app-sider"
    >
      <!-- Logo -->
      <div class="sider-header">
        <img
          class="sider-logo-img"
          src="/talentpilot-logo.svg"
          alt="TalentPilot"
          @error="e => e.target.style.display='none'"
        >
        <div v-if="!collapsed" class="sider-logo-text">
          <div>TalentPilot</div>
          <div class="sider-logo-sub">AI 智能招聘平台</div>
        </div>
      </div>

      <!-- Flat Menu Navigation -->
      <div class="sider-menu-wrapper">
        <!-- Group 1: 职位与简历管理 -->
        <div class="menu-group">
          <div class="menu-group-label" v-if="!collapsed">
            <span class="menu-group-icon">📊</span>
            <span>职位与简历管理</span>
          </div>
          <div class="menu-group-divider" v-if="collapsed"></div>

          <div class="menu-items">
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('dashboard') }"
              @click="navigate('/')"
            >
              <DashboardOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">Dashboard</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('jobposts') }"
              @click="navigate('/jobposts')"
            >
              <BankOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">职位管理</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('resumes') }"
              @click="navigate('/resumes')"
            >
              <FileTextOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">简历管理</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('candidates') }"
              @click="navigate('/candidates')"
            >
              <TeamOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">候选人管理</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('matches') }"
              @click="navigate('/matches')"
            >
              <SafetyOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">智能匹配</span>
            </div>
          </div>
        </div>

        <!-- Group 2: 面试与评估 -->
        <div class="menu-group">
          <div class="menu-group-label" v-if="!collapsed">
            <span class="menu-group-icon">🗂</span>
            <span>面试与评估</span>
          </div>
          <div class="menu-group-divider" v-if="collapsed"></div>

          <div class="menu-items">
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('interviews') }"
              @click="navigate('/interviews')"
            >
              <CalendarOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">面试管理</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('notifications') }"
              @click="navigate('/notifications')"
            >
              <BellOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">通知日志</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('notification-templates') }"
              @click="navigate('/notifications/templates')"
            >
              <FileTextOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">通知模板</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('evaluations') }"
              @click="navigate('/evaluations')"
            >
              <FormOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">评估管理</span>
            </div>
          </div>
        </div>

        <!-- Group 3: 系统管理 -->
        <div class="menu-group">
          <div class="menu-group-label" v-if="!collapsed">
            <span class="menu-group-icon">⚙️</span>
            <span>系统管理</span>
          </div>
          <div class="menu-group-divider" v-if="collapsed"></div>

          <div class="menu-items">
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('users') }"
              @click="navigate('/users')"
            >
              <UserOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">用户账号管理</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('roles') }"
              @click="navigate('/roles')"
            >
              <SafetyCertificateOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">角色和权限管理</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('departments') }"
              @click="navigate('/departments')"
            >
              <ApartmentOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">部门管理</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('operation-logs') }"
              @click="navigate('/operation-logs')"
            >
              <FileTextOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">系统操作日志</span>
            </div>
            <div
              class="menu-item"
              :class="{ active: selectedKeys.includes('channels') }"
              @click="navigate('/channels')"
            >
              <ApiOutlined class="menu-icon" />
              <span class="menu-label" v-if="!collapsed">渠道账号管理</span>
            </div>
          </div>
        </div>
      </div>
    </a-layout-sider>

    <a-layout>
      <!-- Header -->
      <a-layout-header class="app-header">
        <div class="header-left">
          <menu-unfold-outlined v-if="collapsed" class="trigger" @click="toggleCollapsed" />
          <menu-fold-outlined v-else class="trigger" @click="toggleCollapsed" />
          <a-breadcrumb class="header-breadcrumb">
            <a-breadcrumb-item>TalentPilot</a-breadcrumb-item>
            <a-breadcrumb-item>{{ currentTitle }}</a-breadcrumb-item>
          </a-breadcrumb>
        </div>
        <div class="header-right">
          <a-badge :count="0" :offset="[-2, 2]">
            <bell-outlined class="header-icon-btn" />
          </a-badge>
          <a-dropdown>
            <div class="header-user">
              <a-avatar :size="28" class="header-user-avatar">
                {{ userInfo?.username?.charAt(0)?.toUpperCase() || 'U' }}
              </a-avatar>
              <span class="header-user-name">{{ userInfo?.username || 'User' }}</span>
              <down-outlined style="font-size: 10px; color: #d9d9d9;" />
            </div>
            <template #overlay>
              <a-menu>
                <a-menu-item key="profile">
                  <user-outlined /> 个人中心
                </a-menu-item>
                <a-menu-divider />
                <a-menu-item key="logout" @click="handleLogout">
                  <logout-outlined /> 退出登录
                </a-menu-item>
              </a-menu>
            </template>
          </a-dropdown>
        </div>
      </a-layout-header>

      <!-- Content -->
      <a-layout-content class="app-content">
        <router-view />
      </a-layout-content>
    </a-layout>
  </a-layout>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { message } from 'ant-design-vue'
import { useAuthStore } from '@/stores/auth'
import { useAppStore } from '@/stores/app'
import { userApi } from '@/api'
import {
  MenuUnfoldOutlined,
  MenuFoldOutlined,
  DashboardOutlined,
  BankOutlined,
  TeamOutlined,
  SafetyOutlined,
  SafetyCertificateOutlined,
  UserOutlined,
  LogoutOutlined,
  ApartmentOutlined,
  FileTextOutlined,
  CalendarOutlined,
  FormOutlined,
  BellOutlined,
  DownOutlined,
  ApiOutlined
} from '@ant-design/icons-vue'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const appStore = useAppStore()

const collapsed = ref(false)
const selectedKeys = ref(['dashboard'])
const userInfo = ref(null)

const routeTitleMap = {
  '/': 'Dashboard',
  '/jobposts': '职位管理',
  '/resumes': '简历管理',
  '/candidates': '候选人管理',
  '/matches': '智能匹配',
  '/interviews': '面试管理',
  '/evaluations': '评估管理',
  '/users': '用户账号管理',
  '/roles': '角色和权限管理',
  '/departments': '部门管理',
  '/operation-logs': '系统操作日志',
  '/channels': '渠道账号管理'
}

const currentTitle = computed(() => routeTitleMap[route.path] || 'Dashboard')

const toggleCollapsed = () => {
  collapsed.value = !collapsed.value
}

const navigate = (path) => {
  router.push(path)
}

async function handleLogout() {
  await authStore.logout()
  message.success('已退出登录')
  router.push('/login')
}

const handleMenuClick = ({ key }) => {
  router.push(`/${key}`)
}

// Sync selected menu with route
watch(
  () => route.path,
  (path) => {
    const key = path.replace('/', '')
    selectedKeys.value = [key || 'dashboard']
  },
  { immediate: true }
)

onMounted(async () => {
  try {
    const res = await userApi.getCurrentUser()
    userInfo.value = res.data || res
    authStore.setUserInfo(userInfo.value)
  } catch (e) {
    console.error('Failed to get user info:', e)
  }

  try {
    await appStore.fetchMenu()
  } catch (e) {
    console.error('Failed to fetch menu:', e)
  }
})
</script>

<style scoped>
.app-layout {
  min-height: 100vh;
}

.app-sider {
  background: #fff !important;
  box-shadow: 2px 0 12px rgba(0, 0, 0, 0.08);
}

.sider-header {
  height: 60px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  background: #0D3D92;
  gap: 10px;
}

.sider-logo-img {
  width: 32px;
  height: 32px;
  object-fit: contain;
  flex-shrink: 0;
}

.sider-logo-text {
  color: #fff;
  font-size: 13px;
  font-weight: 600;
  line-height: 1.3;
  white-space: nowrap;
}

.sider-logo-sub {
  color: rgba(255, 255, 255, 0.6);
  font-size: 10px;
}

/* Flat Menu Styles */
.sider-menu-wrapper {
  height: calc(100vh - 60px);
  overflow-y: auto;
  padding: 12px 0;
}

.menu-group {
  margin-bottom: 8px;
}

.menu-group-label {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  font-size: 11px;
  font-weight: 600;
  color: #9CA3AF;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.menu-group-icon {
  font-size: 12px;
}

.menu-group-divider {
  height: 1px;
  background: #E5E7EB;
  margin: 8px 12px;
}

.menu-items {
  display: flex;
  flex-direction: column;
}

.menu-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 16px;
  margin: 2px 8px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s ease;
  color: #4B5563;
  font-size: 14px;
  border-left: 3px solid transparent;
}

.menu-item:hover {
  background: rgba(13, 61, 146, 0.06);
  color: #0D3D92;
}

.menu-item.active {
  background: rgba(13, 61, 146, 0.08);
  color: #0D3D92;
  border-left-color: #0D3D92;
  font-weight: 500;
}

.menu-icon {
  font-size: 16px;
  flex-shrink: 0;
  width: 20px;
  text-align: center;
}

.menu-label {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Collapsed state adjustments */
.app-sider :deep(.ant-layout-sider-collapsed) .menu-group-label {
  display: none;
}

.app-sider :deep(.ant-layout-sider-collapsed) .menu-item {
  justify-content: center;
  padding: 10px;
  margin: 2px 6px;
}

.app-sider :deep(.ant-layout-sider-collapsed) .menu-group-divider {
  margin: 8px 6px;
}

/* Header */
.app-header {
  background: #fff !important;
  padding: 0 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);
  border-bottom: 1px solid #E5E7EB;
  height: 60px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
  flex: 1;
}

.trigger {
  font-size: 18px;
  cursor: pointer;
  color: #6B7280;
}

.trigger:hover {
  color: #0D3D92;
}

.header-breadcrumb {
  font-size: 14px;
}

:deep(.ant-breadcrumb-link) {
  color: #6B7280;
}

:deep(.ant-breadcrumb li:last-child .ant-breadcrumb-link) {
  color: #1F2937;
  font-weight: 600;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-icon-btn {
  font-size: 18px;
  color: #6B7280;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 6px;
  transition: all 0.15s;
}

.header-icon-btn:hover {
  color: #0D3D92;
  background: rgba(13, 61, 146, 0.06);
}

.header-user {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.15s;
}

.header-user:hover {
  background: rgba(0, 0, 0, 0.04);
}

.header-user-avatar {
  background: #0D3D92;
  color: #fff;
  font-size: 12px;
  font-weight: 600;
}

.header-user-name {
  font-size: 13px;
  color: #1F2937;
}

.app-content {
  margin: 0;
  padding: 20px 24px;
  background: #F5F7FA;
  min-height: calc(100vh - 60px);
}
</style>
