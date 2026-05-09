import api from './index'

const BASE = '/channel-credentials'

export const channelApi = {
  list: () => api.get(BASE),

  getByType: (channelType) => api.get(`${BASE}/${channelType}`),

  create: (data) => api.post(BASE, data),

  update: (channelType, data) => api.put(`${BASE}/${channelType}`, data),

  remove: (channelType) => api.delete(`${BASE}/${channelType}`),

  validate: (channelType) => api.post(`${BASE}/${channelType}/validate`)
}

export default channelApi
