const API_BASE = '/api';

export const api = {
  login: (username: string, password: string) =>
    fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    }).then((r) => r.json()),

  getCandidates: () => fetch(`${API_BASE}/candidates`).then((r) => r.json()),
  getInterviews: () => fetch(`${API_BASE}/ai-interview-sessions`).then((r) => r.json()),
  getReports: () => fetch(`${API_BASE}/interview-reports`).then((r) => r.json()),
};
