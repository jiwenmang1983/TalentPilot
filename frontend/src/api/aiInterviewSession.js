import api from './index'

export const aiInterviewSessionApi = {
  list: (params) => api.get('/ai-interview-sessions', { params }),
  getById: (id) => api.get(`/ai-interview-sessions/${id}`),
  getByToken: (token) => api.get(`/ai-interview-sessions/by-token/${token}`),
  create: (data) => api.post('/ai-interview-sessions', data),
  start: (id) => api.put(`/ai-interview-sessions/${id}/start`),
  complete: (id) => api.put(`/ai-interview-sessions/${id}/complete`),
  cancel: (id) => api.put(`/ai-interview-sessions/${id}/cancel`),
  submitAnswer: (id, data) => api.post(`/ai-interview-sessions/${id}/submit-answer`, data),
  getNextQuestion: (id) => api.get(`/ai-interview-sessions/${id}/next-question`)
}
