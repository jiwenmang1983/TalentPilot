import api from './index'

export const interviewReportApi = {
  list: (params) => api.get('/interview-reports', { params }),
  getById: (id) => api.get(`/interview-reports/${id}`),
  getBySession: (sessionId) => api.get(`/interview-reports/session/${sessionId}`),
  generate: (sessionId) => api.post(`/interview-reports/generate/${sessionId}`),
  updateNotes: (id, hrNotes) => api.put(`/interview-reports/${id}/notes`, { hrNotes })
}
