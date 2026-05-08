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

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

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
  disable: (id) => api.post(`/users/${id}/disable`),
  enable: (id) => api.post(`/users/${id}/enable`),
  resetPassword: (id) => api.post(`/users/${id}/reset-password`)
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

export default api