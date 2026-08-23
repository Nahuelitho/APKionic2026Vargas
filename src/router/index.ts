import { createRouter, createWebHistory } from '@ionic/vue-router';
import { RouteRecordRaw } from 'vue-router';

import MainLayout from '../layouts/main_layout.vue';
import { navegacion, type ItemNavegacion } from '../config/navegacion';

const rutasApp = navegacion.map((item: ItemNavegacion) => ({
  path: item.ruta.replace('/app/', ''),
  name: item.id,
  component: item.componente,
}));

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    redirect: '/app/inicio',
  },
  {
    path: '/app',
    component: MainLayout,
    children: rutasApp,
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/app/inicio',
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

export default router;