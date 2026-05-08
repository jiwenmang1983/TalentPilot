<template>
  <a-layout class="main-layout">
    <a-layout-sider
      v-model:collapsed="collapsed"
      :trigger="null"
      collapsible
      class="sidebar"
    >
      <div class="logo">
        <span v-if="!collapsed">TalentPilot</span>
        <span v-else>TP</span>
      </div>
      <a-menu
        v-model:selectedKeys="selectedKeys"
        theme="dark"
        mode="inline"
        :items="menuItems"
        @click="handleMenuClick"
      />
    </a-layout-sider>

    <a-layout>
      <a-layout-header class="header">
        <menu-unfold-outlined
          v-if="collapsed"
          class="trigger"
          @click="toggleCollapsed"
        />
        <menu-fold-outlined
          v-else
          class="trigger"
          @click="toggleCollapsed"
        />

        <div class="header-right">
          <span class="username">{{ userInfo?.username || 'User' }}</span>
          <a-dropdown>
            <a-avatar class="avatar">
              <template #icon><UserOutlined /></template>
            </a-avatar>
            <template #overlay>
              <a-menu>
                <a-menu-item key="profile">
                  <UserOutlined /> 个人中心
                </a-menu-item>
                <a-menu-divider />
                <a-menu-item key="logout" @click="handleLogout">
                  <LogoutOutlined /> 退出登录
                </a-menu-item>
              </a-menu>
            </template>
          </a-dropdown>
        </div>
      </a-layout-header>

      <a-layout-content class="content">
        <router-view />
      </a-layout-content>
    </a-layout>
  </a-layout>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import {
  MenuUnfoldOutlined,
  MenuFoldOutlined,
  UserOutlined,
  LogoutOutlined,
  BankOutlined,
  TeamOutlined,
  SafetyOutlined,
  ApartmentOutlined,
  FileTextOutlined
} from '@ant-design/icons-vue'
import { useAuthStore } from '@/stores/auth'
import { useAppStore } from '@/stores/app'
import { userApi } from '@/api'
import { message } from 'ant-design-vue'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const appStore = useAppStore()

const collapsed = ref(false)
const selectedKeys = ref([route.name])
const userInfo = ref(null)

const menuItems = computed(() => {
  const menus = appStore.menuData.length > 0 ? appStore.menuData : [
    { key: 'users', label: '用户管理', icon: 'UserOutlined' },
    { key: 'roles', label: '角色管理', icon: 'SafetyOutlined' },
    { key: 'departments', label: '部门管理', icon: 'ApartmentOutlined' },
    { key: 'logs', label: '操作日志', icon: 'FileTextOutlined' },
    { key: 'jobposts', label: '职位管理', icon: 'BankOutlined' },
    { key: 'resumes', label: '简历管理', icon: 'FileTextOutlined' },
    { key: 'candidates', label: '候选人管理', icon: 'TeamOutlined' },
    { key: 'matches', label: '智能匹配', icon: 'TeamOutlined' }
  ]
  return menus.map(item => ({
    key: item.path || item.key,
    label: item.title || item.label,
    icon: item.icon ? () => h(resolveComponent(item.icon)) : null
  }))
})

import { h, resolveComponent } from 'vue'

function toggleCollapsed() {
  collapsed.value = !collapsed.value
}

function handleMenuClick({ key }) {
  router.push(`/${key}`)
}

async function handleLogout() {
  await authStore.logout()
  message.success('已退出登录')
  router.push('/login')
}

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
.main-layout {
  min-height: 100vh;
}

.sidebar {
  min-height: 100vh;
}

.logo {
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 20px;
  font-weight: bold;
  background: rgba(255, 255, 255, 0.1);
}

.header {
  background: white;
  padding: 0 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
}

.trigger {
  font-size: 18px;
  cursor: pointer;
  padding: 0 12px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.username {
  color: #333;
}

.avatar {
  cursor: pointer;
}

.content {
  margin: 16px;
  padding: 24px;
  background: white;
  border-radius: 8px;
  min-height: calc(100vh - 64px - 32px);
}
</style>