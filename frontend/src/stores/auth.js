import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('accessToken') || '')
  const userInfo = ref(null)

  const isLoggedIn = computed(() => !!token.value)

  async function login(username, password) {
    const res = await authApi.login(username, password)
    const accessToken = res.data?.accessToken || res.accessToken
    const userData = res.data?.user || res.user
    const refresh = res.data?.refreshToken || res.refreshToken
    if (!accessToken) {
      const err = new Error(res.message || res.msg || '登录失败，用户名或密码错误')
      err.response = { data: { message: err.message } }
      throw err
    }
    token.value = accessToken
    localStorage.setItem('accessToken', accessToken)
    if (refresh) {
      localStorage.setItem('refreshToken', refresh)
    }
    if (userData) {
      userInfo.value = userData
      localStorage.setItem('userInfo', JSON.stringify(userData))
    }
    return true
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