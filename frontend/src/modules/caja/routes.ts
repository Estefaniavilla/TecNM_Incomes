import type { RouteRecordRaw } from 'vue-router'

export const cajaRoutes: RouteRecordRaw[] = [
  {
    path: 'caja/recibo',
    name: 'caja-recibo',
    component: () => import('./views/CajaPrincipalView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: 'caja/indicadores',
    name: 'caja-indicadores',
    component: () => import('./views/DashboardIndicadoresView.vue'),
    meta: { requiresAuth: true },
  },
]

