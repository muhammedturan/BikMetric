<template>
  <div class="login-page">
    <div class="login-card">
      <div class="login-header">
        <span class="login-logo">📊</span>
        <h1>BikMetric</h1>
        <p>AI-Powered News Analytics</p>
      </div>
      <form @submit.prevent="handleLogin" class="login-form">
        <div class="field">
          <label>E-posta</label>
          <input v-model="email" type="email" placeholder="admin@bikmetric.com" required />
        </div>
        <div class="field">
          <label>Sifre</label>
          <input v-model="password" type="password" placeholder="------" required />
        </div>
        <p v-if="error" class="error">{{ error }}</p>
        <button type="submit" class="login-btn" :disabled="loading">
          {{ loading ? 'Giris yapiliyor...' : 'Giris Yap' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/authStore'

const authStore = useAuthStore()
const router = useRouter()

const email = ref('admin@bikmetric.com')
const password = ref('admin123')
const error = ref('')
const loading = ref(false)

async function handleLogin() {
  error.value = ''
  loading.value = true
  try {
    await authStore.login(email.value, password.value)
    router.push('/')
  } catch (e: any) {
    error.value = e.response?.data?.message || 'Giris basarisiz'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--dark) 0%, #312e81 100%);
}

.login-card {
  background: white;
  border-radius: var(--radius-lg);
  padding: 40px;
  width: 400px;
  box-shadow: var(--shadow-lg);
}

.login-header { text-align: center; margin-bottom: 32px; }
.login-logo { font-size: 48px; display: block; margin-bottom: 12px; }
.login-header h1 { font-size: 24px; color: var(--gray-900); margin-bottom: 4px; }
.login-header p { color: var(--gray-500); font-size: 14px; }

.login-form { display: flex; flex-direction: column; gap: 16px; }

.field { display: flex; flex-direction: column; gap: 6px; }
.field label { font-size: 13px; font-weight: 600; color: var(--gray-700); }
.field input {
  padding: 10px 12px;
  border: 1px solid var(--gray-200);
  border-radius: var(--radius);
  font-size: 14px;
  transition: border-color 0.15s;
}
.field input:focus { border-color: var(--primary); box-shadow: 0 0 0 3px var(--primary-light); }

.error { color: var(--danger); font-size: 13px; text-align: center; }

.login-btn {
  padding: 12px;
  background: var(--primary);
  color: white;
  border-radius: var(--radius);
  font-size: 14px;
  font-weight: 600;
  transition: background 0.15s;
}
.login-btn:hover:not(:disabled) { background: var(--primary-hover); }
.login-btn:disabled { opacity: 0.6; cursor: not-allowed; }
</style>
