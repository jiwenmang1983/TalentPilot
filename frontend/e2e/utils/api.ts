const API_BASE_URL = 'http://localhost:5010/api'

export interface ApiResponse<T = unknown> {
  data?: T
  accessToken?: string
  refreshToken?: string
  message?: string
}

export async function apiLogin(username: string, password: string): Promise<{ accessToken: string; refreshToken?: string }> {
  const response = await fetch(`${API_BASE_URL}/auth/login`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ username, password }),
  })

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.message || `Login failed: ${response.status}`)
  }

  const data = await response.json()
  return {
    accessToken: data.accessToken,
    refreshToken: data.refreshToken,
  }
}

export async function apiGet<T>(endpoint: string, token: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  })

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status}`)
  }

  const data = await response.json()
  return data.data ?? data
}

export async function apiPost<T>(endpoint: string, body: unknown, token: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(body),
  })

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.message || `API request failed: ${response.status}`)
  }

  const data = await response.json()
  return data.data ?? data
}

export async function apiPut<T>(endpoint: string, body: unknown, token: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(body),
  })

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.message || `API request failed: ${response.status}`)
  }

  const data = await response.json()
  return data.data ?? data
}

export async function apiDelete<T>(endpoint: string, token: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    method: 'DELETE',
    headers: {
      Authorization: `Bearer ${token}`,
    },
  })

  if (!response.ok) {
    const error = await response.json()
    throw new Error(error.message || `API request failed: ${response.status}`)
  }

  const data = await response.json()
  return data.data ?? data
}

export { API_BASE_URL }
