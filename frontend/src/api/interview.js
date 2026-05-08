import api from './index'

export const interviewApi = {
  list: (params) => api.get('/interview-invitations', { params }),
  getById: (id) => api.get(`/interview-invitations/${id}`),
  getByToken: (token) => api.get(`/interview-invitations/by-token/${token}`),
  create: (data) => api.post('/interview-invitations', data),
  update: (id, data) => api.put(`/interview-invitations/${id}`, data),
  delete: (id) => api.delete(`/interview-invitations/${id}`),
  send: (id) => api.post(`/interview-invitations/${id}/send`),
  confirm: (id, data) => api.post(`/interview-invitations/${id}/confirm`, data),
  refuse: (id) => api.post(`/interview-invitations/${id}/refuse`),
  cancel: (id) => api.post(`/interview-invitations/${id}/cancel`)
}
