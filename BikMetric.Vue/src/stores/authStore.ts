import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '../services/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const user = ref<any>(null)

  const isAuthenticated = computed(() => !!token.value)

  function initializeAuth() {
    const savedToken = localStorage.getItem('bikmetric_token')
    const savedUser = localStorage.getItem('bikmetric_user')
    if (savedToken && savedUser) {
      token.value = savedToken
      user.value = JSON.parse(savedUser)
    }
  }

  async function login(email: string, password: string) {
    const { data } = await api.post('/auth/login', { email, password })
    token.value = data.token
    user.value = {
      userId: data.userId,
      email: data.email,
      fullName: data.fullName,
      role: data.role,
    }
    localStorage.setItem('bikmetric_token', data.token)
    localStorage.setItem('bikmetric_user', JSON.stringify(user.value))
  }

  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem('bikmetric_token')
    localStorage.removeItem('bikmetric_user')
  }

  return { token, user, isAuthenticated, initializeAuth, login, logout }
})
