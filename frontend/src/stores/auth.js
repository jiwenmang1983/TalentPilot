import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('accessToken') || '')
  const userInfo = ref(null)

  const isLoggedIn = computed(() => !!token.value)

  async function login(username, password) {
    const res = await authApi.login(username, password)
    if (res.accessToken) {
      token.value = res.accessToken
      localStorage.setItem('accessToken', res.accessToken)
      if (res.refreshToken) {
        localStorage.setItem('refreshToken', res.refreshToken)
      }
      if (res.user) {
        userInfo.value = res.user
        localStorage.setItem('userInfo', JSON.stringify(res.user))
      }
      return true
    }
    return false
  }

  async function logout() {
    try {
      await authApi.logout()
    } catch (e) {
      // ignore error
    }
    token.value = ''
    userInfo.value = null
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('userInfo')
  }

  function setUserInfo(user) {
    userInfo.value = user
    if (user) {
      localStorage.setItem('userInfo', JSON.stringify(user))
    }
  }

  function getUserInfo() {
    if (!userInfo.value) {
      const stored = localStorage.getItem('userInfo')
      if (stored) {
        userInfo.value = JSON.parse(stored)
      }
    }
    return userInfo.value
  }

  return {
    token,
    userInfo,
    isLoggedIn,
    login,
    logout,
    setUserInfo,
    getUserInfo
  }
})