import api from './index'

export const matchApi = {
  list: (params) => api.get('/matches', { params }),
  calculate: (data) => api.post('/matches/calculate', data),
  updateStatus: (id, status) => api.patch(`/matches/${id}/status`, { status })
}
