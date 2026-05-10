import api from './index'

export const matchResultApi = {
  list: (params) => api.get('/matches', { params }),
  getById: (id) => api.get(`/matches/${id}`),
  batchMatch: (jobPostId) => api.post(`/jobposts/${jobPostId}/match`),
  updateStatus: (id, status) => api.patch(`/matches/${id}/status`, { status }),
  getThreshold: (jobPostId) => api.get(`/jobposts/${jobPostId}/match-threshold`),
  setThreshold: (jobPostId, threshold) => api.put(`/jobposts/${jobPostId}/match-threshold`, { threshold }),
  getWeights: (jobPostId) => api.get(`/jobposts/${jobPostId}/match-weights`),
  setWeights: (jobPostId, weights) => api.put(`/jobposts/${jobPostId}/match-weights`, { weights }),
}