import axios from 'axios'

const BASE_URL = '/api'
const BASE = '/distribution'

const distApi = axios.create({
  baseURL: BASE_URL,
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' }
})

// Inject JWT token
distApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Unwrap response data
distApi.interceptors.response.use((response) => response.data)

export const jobDistributionApi = {
  trigger(jobPostId, channelTypes, scheduledAt = null) {
    return distApi.post(BASE + '/trigger', { jobPostId, channelTypes, scheduledAt })
  },

  create(jobPostId, channelTypes, scheduledAt = null) {
    return distApi.post(BASE + '/tasks', { jobPostId, channelTypes, scheduledAt })
  },

  getByJob(jobPostId) {
    return distApi.get(BASE + `/tasks/job/${jobPostId}`)
  },

  getTask(taskId) {
    return distApi.get(BASE + `/tasks/${taskId}`)
  },

  getLogs(taskId) {
    return distApi.get(BASE + `/tasks/${taskId}/logs`)
  },

  getLogsByJob(jobPostId) {
    return distApi.get(BASE + `/logs/job/${jobPostId}`)
  },

  retry(taskId) {
    return distApi.put(BASE + `/tasks/${taskId}/retry`)
  },

  cancel(taskId) {
    return distApi.put(BASE + `/tasks/${taskId}/cancel`)
  }
}
