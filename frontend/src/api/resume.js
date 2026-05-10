import api from './index'

export const resumeApi = {
  list: (params) => api.get('/resumes', { params }),
  getById: (id) => api.get(`/resumes/${id}`),
  upload: (data) => api.post('/resumes/upload', data),
  mockCollect: (data) => api.post('/resumes/mock-collect', data),
  parse: (id) => api.post(`/resumes/${id}/parse`),
  parseText: (data) => api.post('/resumes/parse', data),
  createCandidate: (data) => api.post('/candidates', data),
  collectNow: (params) => api.post('/resumes/collect-now', params || {}),
  getSources: () => api.get('/resumes/channels'),
  overrideMatchThreshold: (matchId, overrideThreshold) =>
    api.patch(`/matches/${matchId}/threshold`, { overrideThreshold })
}
