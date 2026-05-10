import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/auth/Login.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: () => import('@/views/layout/MainLayout.vue'),
    meta: { requiresAuth: true },
    redirect: '/dashboard',
    children: [
      {
        path: '',
        redirect: '/dashboard'
      },
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/analytics/Dashboard.vue'),
        meta: { title: 'Dashboard', icon: 'DashboardOutlined' }
      },
      {
        path: 'users',
        name: 'UserManagement',
        component: () => import('@/views/system/UserManagement.vue'),
        meta: { title: '用户管理', icon: 'UserOutlined' }
      },
      {
        path: 'roles',
        name: 'RoleManagement',
        component: () => import('@/views/system/RoleManagement.vue'),
        meta: { title: '角色管理', icon: 'SafetyOutlined' }
      },
      {
        path: 'departments',
        name: 'DepartmentTree',
        component: () => import('@/views/system/DepartmentTree.vue'),
        meta: { title: '部门管理', icon: 'ApartmentOutlined' }
      },
      {
        path: 'logs',
        name: 'OperationLogs',
        component: () => import('@/views/system/OperationLogs.vue'),
        meta: { title: '操作日志', icon: 'FileTextOutlined' }
      },
      {
        path: 'channels',
        name: 'ChannelCredentialManagement',
        component: () => import('@/views/system/ChannelCredentialManagement.vue'),
        meta: { title: '渠道账号管理', icon: 'ApiOutlined' }
      },
      {
        path: 'jobposts',
        name: 'JobPostList',
        component: () => import('@/views/recruitment/JobPostList.vue'),
        meta: { title: '职位管理', icon: 'BankOutlined' }
      },
      {
        path: 'jobposts/new',
        name: 'JobPostCreate',
        component: () => import('@/views/recruitment/JobPostForm.vue'),
        meta: { title: '新建职位', icon: 'BankOutlined' }
      },
      {
        path: 'jobposts/:id',
        name: 'JobPostEdit',
        component: () => import('@/views/recruitment/JobPostForm.vue'),
        meta: { title: '编辑职位', icon: 'BankOutlined' }
      },
      {
        path: 'resumes',
        name: 'ResumeList',
        component: () => import('@/views/recruitment/ResumeList.vue'),
        meta: { title: '简历管理', icon: 'FileTextOutlined' }
      },
      {
        path: 'resumes/parse',
        name: 'ResumeParse',
        component: () => import('@/views/recruitment/ResumeParse.vue'),
        meta: { title: '简历解析', icon: 'ScanOutlined' }
      },
      {
        path: 'candidates',
        name: 'CandidateList',
        component: () => import('@/views/recruitment/CandidateList.vue'),
        meta: { title: '候选人管理', icon: 'TeamOutlined' }
      },
      {
        path: 'candidates/:id',
        name: 'CandidateDetail',
        component: () => import('@/views/recruitment/CandidateDetail.vue'),
        meta: { title: '候选人详情', icon: 'TeamOutlined' }
      },
      {
        path: 'matches',
        name: 'MatchResultList',
        component: () => import('@/views/recruitment/MatchResultList.vue'),
        meta: { title: '智能匹配', icon: 'TeamOutlined' }
      },
      {
        path: 'matches/:id',
        name: 'MatchResultDetail',
        component: () => import('@/views/recruitment/MatchResultDetail.vue'),
        meta: { title: '匹配详情', icon: 'TeamOutlined' }
      },
      {
        path: 'recruitment/interviews',
        name: 'InterviewInvitationList',
        component: () => import('@/views/recruitment/InterviewInvitationList.vue'),
        meta: { title: '面试邀约', icon: 'ScheduleOutlined' }
      },
      {
        path: 'interview/sessions',
        name: 'InterviewSessions',
        component: () => import('@/views/interview/InterviewSessions.vue'),
        meta: { title: 'AI面试会话', icon: 'VideoCameraOutlined' }
      },
      {
        path: 'interview/reports',
        name: 'InterviewReports',
        component: () => import('@/views/interview/InterviewReports.vue'),
        meta: { title: '面试报告', icon: 'FileTextOutlined' }
      },
      {
        path: 'notifications',
        name: 'NotificationList',
        component: () => import('@/views/notification/NotificationList.vue'),
        meta: { title: '通知日志', icon: 'BellOutlined' }
      },
      {
        path: 'notifications/templates',
        name: 'NotificationTemplates',
        component: () => import('@/views/notification/NotificationTemplates.vue'),
        meta: { title: '通知模板', icon: 'FileTextOutlined' }
      }
    ]
  },
  {
    path: '/interview/confirm/:token',
    name: 'CandidateConfirm',
    component: () => import('@/views/interview/CandidateConfirm.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/interview/candidate',
    name: 'CandidateInterview',
    component: () => import('@/views/interview/CandidateInterview.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/interview-book/:sessionToken',
    name: 'InterviewBooking',
    component: () => import('@/views/interview/InterviewBooking.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/interview/candidate-dashboard',
    name: 'CandidateDashboard',
    component: () => import('@/views/interview/CandidateDashboard.vue'),
    meta: { requiresAuth: false }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth !== false)

  if (requiresAuth && !authStore.isLoggedIn) {
    next('/login')
  } else if (to.path === '/login' && authStore.isLoggedIn) {
    next('/')
  } else {
    next()
  }
})

export default router
