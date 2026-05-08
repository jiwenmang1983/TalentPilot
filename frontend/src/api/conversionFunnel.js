import api from './index'

export const conversionFunnelApi = {
  list: (params) => api.get('/conversion-funnels', { params }),
  summary: (params) => api.get('/conversion-funnels/summary', { params }),
  chart: (params) => api.get('/conversion-funnels/chart', { params }),
  seed: () => api.post('/conversion-funnels/seed')
}