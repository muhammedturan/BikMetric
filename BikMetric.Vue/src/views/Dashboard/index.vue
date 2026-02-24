<template>
  <div class="dashboard">
    <h1 class="page-title">Dashboard</h1>
    <div class="cards">
      <div class="stat-card">
        <div class="stat-icon" style="background: var(--primary-light); color: var(--primary);">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" width="24" height="24"><path stroke-linecap="round" stroke-linejoin="round" d="M12 3c2.755 0 5.455.232 8.083.678.533.09.917.556.917 1.096v1.044a2.25 2.25 0 0 1-.659 1.591l-5.432 5.432a2.25 2.25 0 0 0-.659 1.591v2.927a2.25 2.25 0 0 1-1.244 2.013L9.75 21v-6.568a2.25 2.25 0 0 0-.659-1.591L3.659 7.409A2.25 2.25 0 0 1 3 5.818V4.774c0-.54.384-1.006.917-1.096A48.32 48.32 0 0 1 12 3Z" /></svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Kayitli Kurallar</span>
          <span class="stat-value">{{ ruleStore.rules.length }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon" style="background: #dcfce7; color: var(--success);">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" width="24" height="24"><path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904 9 18.75l-.813-2.846a4.5 4.5 0 0 0-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 0 0 3.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 0 0 3.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 0 0-3.09 3.09Z" /></svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">AI Sorgu</span>
          <span class="stat-value">Hazir</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon" style="background: #fef3c7; color: var(--warning);">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" width="24" height="24"><path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z" /></svg>
        </div>
        <div class="stat-content">
          <span class="stat-label">Chart Wizard</span>
          <span class="stat-value">Hazir</span>
        </div>
      </div>
    </div>
    <div class="welcome-card">
      <h2>Hos Geldiniz!</h2>
      <p>BikMetric ile haber verilerinizi dogal dil kullanarak sorgulayabilir, kurallar olusturabilir ve grafikler uretebilirsiniz.</p>
      <div class="quick-links">
        <router-link to="/ai-query" class="quick-link primary">
          AI ile Sorgula
        </router-link>
        <router-link to="/rules" class="quick-link">
          Kural Olustur
        </router-link>
        <router-link to="/chart-wizard" class="quick-link">
          Grafik Olustur
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRuleStore } from '../../stores/ruleStore'

const ruleStore = useRuleStore()
onMounted(() => ruleStore.loadRules())
</script>

<style scoped>
.page-title { font-size: 24px; font-weight: 700; margin-bottom: 24px; }

.cards { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 24px; }

.stat-card {
  background: white; border-radius: var(--radius-lg); padding: 20px;
  box-shadow: var(--shadow); display: flex; align-items: center; gap: 16px;
}

.stat-icon {
  width: 48px; height: 48px; border-radius: var(--radius);
  display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}

.stat-label { font-size: 13px; color: var(--gray-500); display: block; }
.stat-value { font-size: 24px; font-weight: 700; }

.welcome-card {
  background: white; border-radius: var(--radius-lg); padding: 32px;
  box-shadow: var(--shadow);
}
.welcome-card h2 { font-size: 20px; margin-bottom: 8px; }
.welcome-card p { color: var(--gray-500); font-size: 14px; margin-bottom: 20px; line-height: 1.6; }

.quick-links { display: flex; gap: 12px; }
.quick-link {
  padding: 10px 20px; border-radius: var(--radius); font-size: 14px;
  font-weight: 500; background: var(--gray-100); color: var(--gray-700);
  transition: all 0.15s;
}
.quick-link:hover { background: var(--gray-200); }
.quick-link.primary { background: var(--primary); color: white; }
.quick-link.primary:hover { background: var(--primary-hover); }
</style>
