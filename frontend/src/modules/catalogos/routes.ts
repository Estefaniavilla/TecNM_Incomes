import type { RouteRecordRaw } from 'vue-router'

export const catalogosRoutes: RouteRecordRaw[] = [
  {
    path: 'catalogos/empleados',
    name: 'catalogos-empleados',
    component: () => import('./views/EmpleadosView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: 'catalogos/cuentas',
    name: 'catalogos-cuentas',
    component: () => import('./views/CuentasView.vue'),
    meta: { requiresAuth: true },
  },
]

