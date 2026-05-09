import axios from 'axios'

const BASE = '/distribution'

export const jobDistributionApi = {
  trigger(jobPostId, channelTypes, scheduledAt = null) {
    return axios.post(BASE + '/trigger', {
      jobPostId,
      channelTypes,
      scheduledAt
    })
  },

  create(jobPostId, channelTypes, scheduledAt = null) {
    return axios.post(BASE + '/tasks', {
      jobPostId,
      channelTypes,
      scheduledAt
    })
  },

  getByJob(jobPostId) {
    return axios.get(BASE + `/tasks/job/${jobPostId}`)
  },

  getTask(taskId) {
    return axios.get(BASE + `/tasks/${taskId}`)
  },

  getLogs(taskId) {
    return axios.get(BASE + `/tasks/${taskId}/logs`)
  },

  getLogsByJob(jobPostId) {
    return axios.get(BASE + `/logs/job/${jobPostId}`)
  },

  retry(taskId) {
    return axios.put(BASE + `/tasks/${taskId}/retry`)
  },

  cancel(taskId) {
    return axios.delete(BASE + `/tasks/${taskId}`)
  }
}