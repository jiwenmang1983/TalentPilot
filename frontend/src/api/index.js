import axios from 'axios'
import { message } from 'ant-design-vue'

const API_BASE_URL = 'http://localhost:5010/api'

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

api.interceptors.response.use(
  (response) => {
    return response.data
  },
  async (error) => {
    const originalRequest = error.config
    console.log('[API Interceptor] Error caught:', error.response?.status, error.response?.data)

    // Skip token refresh logic for login endpoint (user not authenticated yet)
    const isLoginRequest = originalRequest.url?.includes('/auth/login')
    console.log('[API Interceptor] isLoginRequest:', isLoginRequest)

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      // For login requests, don't redirect - let the error propagate to caller
      if (isLoginRequest) {
        console.log('[API Interceptor] Login 401 - showing message and rejecting')
        if (error.response?.data?.message) {
          console.log('[API Interceptor] Calling message.error:', error.response.data.message)
          message.error(error.response.data.message)
        } else {
          console.log('[API Interceptor] No message in response, using default')
          message.error('登录失败，用户名或密码错误')
        }
        return Promise.reject(error)
      }

      const refreshToken = localStorage.getItem('refreshToken')
      if (refreshToken) {
        try {
          const res = await axios.post(`${API_BASE_URL}/auth/refresh`, {
            refreshToken
          })

          if (res.data.accessToken) {
            localStorage.setItem('accessToken', res.data.accessToken)
            if (res.data.refreshToken) {
              localStorage.setItem('refreshToken', res.data.refreshToken)
            }
            originalRequest.headers.Authorization = `Bearer ${res.data.accessToken}`
            return api(originalRequest)
          }
        } catch (refreshError) {
          localStorage.removeItem('accessToken')
          localStorage.removeItem('refreshToken')
          window.location.href = '/login'
          return Promise.reject(refreshError)
        }
      } else {
        localStorage.removeItem('accessToken')
        localStorage.removeItem('refreshToken')
        window.location.href = '/login'
      }
    }

    if (error.response?.data?.message) {
      message.error(error.response.data.message)
    } else if (error.message) {
      message.error(error.message)
    }

    return Promise.reject(error)
  }
)

export const authApi = {
  login: (username, password) => api.post('/auth/login', { username, password }),
  refresh: (refreshToken) => api.post('/auth/refresh', { refreshToken }),
  logout: () => api.post('/auth/logout')
}

export const userApi = {
  list: (params) => api.get('/users', { params }),
  getById: (id) => api.get(`/users/${id}`),
  getCurrentUser: () => api.get('/users/current'),
  create: (data) => api.post('/users', data),
  update: (id, data) => api.put(`/users/${id}`, data),
  delete: (id) => api.delete(`/users/${id}`),
  toggleActive: (id) => api.post(`/users/${id}/toggle-active`),
  resetPassword: (id, newPassword) => api.post(`/users/${id}/reset-password`, { newPassword })
}

export const roleApi = {
  list: () => api.get('/roles'),
  getById: (id) => api.get(`/roles/${id}`),
  create: (data) => api.post('/roles', data),
  update: (id, data) => api.put(`/roles/${id}`, data),
  delete: (id) => api.delete(`/roles/${id}`),
  getPermissions: (id) => api.get(`/roles/${id}/permissions`),
  updatePermissions: (id, permissions) => api.put(`/roles/${id}/permissions`, { permissions })
}

export const departmentApi = {
  list: () => api.get('/departments'),
  getTree: () => api.get('/departments/tree'),
  getById: (id) => api.get(`/departments/${id}`),
  create: (data) => api.post('/departments', data),
  update: (id, data) => api.put(`/departments/${id}`, data),
  delete: (id) => api.delete(`/departments/${id}`)
}

export const permissionApi = {
  getAll: () => api.get('/permissions/all'),
  getMenu: () => api.get('/permissions/menu')
}

export const operationLogApi = {
  list: (params) => api.get('/operation-logs', { params }),
  getById: (id) => api.get(`/operation-logs/${id}`)
}

export { notificationApi } from './notification'
export { channelApi } from './channel'
export { jobChannelContentApi } from './jobChannelContent'
export { jobDistributionApi } from './jobDistribution'
export { api }

export default api