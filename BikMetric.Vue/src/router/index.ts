import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('../views/Login/index.vue'),
    },
    {
      path: '/',
      component: () => import('../layouts/DashboardLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          name: 'Dashboard',
          component: () => import('../views/Dashboard/index.vue'),
        },
        {
          path: 'rules',
          name: 'Rules',
          component: () => import('../views/Rules/index.vue'),
        },
        {
          path: 'chart-wizard',
          name: 'ChartWizard',
          component: () => import('../views/ChartWizard/index.vue'),
        },
        {
          path: 'ai-query',
          name: 'AiQuery',
          component: () => import('../views/AiQuery/index.vue'),
        },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const token = localStorage.getItem('bikmetric_token')
  if (to.meta.requiresAuth && !token) {
    return { name: 'Login' }
  }
  if (to.name === 'Login' && token) {
    return { path: '/' }
  }
})

export default router
