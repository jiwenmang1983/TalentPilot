import api from './index'

export const notificationApi = {
  list: (params) => api.get('/notifications', { params }),
  send: (data) => api.post('/notifications/send', data),
  getTemplates: () => api.get('/notifications/templates'),
  updateTemplate: (id, data) => api.put(`/notifications/templates/${id}`, data)
}