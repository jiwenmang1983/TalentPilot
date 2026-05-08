import api from './index'

export const jobPostApi = {
  list: (params) => api.get('/jobposts', { params }),
  getById: (id) => api.get(`/jobposts/${id}`),
  create: (data) => api.post('/jobposts', data),
  update: (id, data) => api.put(`/jobposts/${id}`, data),
  updateStatus: (id, status) => api.patch(`/jobposts/${id}/status`, { status }),
  delete: (id) => api.delete(`/jobposts/${id}`)
}
