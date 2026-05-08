import { defineStore } from 'pinia'
import { ref } from 'vue'
import { permissionApi } from '@/api'

export const useAppStore = defineStore('app', () => {
  const menuData = ref([])
  const permissions = ref([])

  async function fetchMenu() {
    try {
      const res = await permissionApi.getMenu()
      menuData.value = res.data || res || []
      return menuData.value
    } catch (e) {
      console.error('Failed to fetch menu:', e)
      return []
    }
  }

  async function fetchPermissions() {
    try {
      const res = await permissionApi.getAll()
      permissions.value = res.data || res || []
      return permissions.value
    } catch (e) {
      console.error('Failed to fetch permissions:', e)
      return []
    }
  }

  return {
    menuData,
    permissions,
    fetchMenu,
    fetchPermissions
  }
})