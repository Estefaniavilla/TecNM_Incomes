import { createRouter, createWebHistory } from 'vue-router'

import { authRoutes, authenticatedAuthRoutes } from '../../modules/auth'
import { cajaRoutes } from '../../modules/caja'
import { catalogosRoutes } from '../../modules/catalogos'
import {
  clearSessionMarker,
  hasActiveSession,
} from '../../shared/utils/sessionCookie'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    ...authRoutes,
    {
      path: '/',
      component: () => import('../layouts/MainLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          name: 'home',
          component: () => import('../../modules/home/views/HomeView.vue'),
          meta: { requiresAuth: true },
        },
        ...cajaRoutes,
        ...catalogosRoutes,
        ...authenticatedAuthRoutes,
      ],
    },
  ],
  scrollBehavior(to, _from, savedPosition) {
    return savedPosition ?? (to.hash ? { el: to.hash, top: 24 } : { top: 0 })
  },
})

router.beforeEach((to) => {
  const isAuthenticated = hasActiveSession()

  // Si el usuario ya inició sesión y visita /login, enviar a la página principal
  if (to.name === 'login' && isAuthenticated) {
    return { path: '/' }
  }

  // Si la ruta requiere autenticación y no hay sesión activa, redirigir siempre al login
  if (to.matched.some((record) => record.meta.requiresAuth) && !isAuthenticated) {
    clearSessionMarker()
    return {
      name: 'login',
      query: { redirect: to.fullPath },
    }
  }

  return true
})

export default router
