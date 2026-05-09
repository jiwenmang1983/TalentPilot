import api from './index'

export const jobChannelContentApi = {
  getByJobPost: (jobPostId) => api.get('/job-channel-contents', { params: { jobPostId } }),
  adapt: (jobPostId, channelTypes) => api.post('/job-channel-contents/adapt', { jobPostId, channelTypes }),
  update: (id, data) => api.put(`/job-channel-contents/${id}`, data),
  remove: (id) => api.delete(`/job-channel-contents/${id}`),
  getByChannelType: (jobPostId, channelType) => api.get(`/job-channel-contents/${channelType}`, { params: { jobPostId } })
}
