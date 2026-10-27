import { createRouter, createWebHistory } from '@ionic/vue-router';
import { RouteRecordRaw } from 'vue-router';

import MainLayout from '../layouts/main_layout.vue';
import { navegacion, ruta_inicial, type ItemNavegacion } from '../config/navegacion';
import LoginPage from '../views/LoginPage.vue';
import CatalogoPage from '../views/CatalogoPage.vue';
import EmpresaDetallePage from '../views/EmpresaDetallePage.vue';
import NotificacionesPage from '../views/NotificacionesPage.vue';
import { restaurar_sesion, sesion } from '../services/auth_service';

const rutasApp = navegacion.map((item: ItemNavegacion) => ({
  path: item.ruta.replace('/app/', ''),
  name: item.id,
  component: item.componente,
  meta: { roles: item.roles },
}));

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    redirect: '/login',
  },
  { path: '/login', component: LoginPage, meta: { publica: true } },
  {
    path: '/app',
    component: MainLayout,
    children: [
      { path: '', redirect: () => ruta_inicial(sesion.usuario.value?.rol) },
      ...rutasApp,
      { path: 'empresas/:id', name: 'empresa-detalle', component: EmpresaDetallePage, meta: { roles: ['superadmin', 'administrador'] } },
      { path: 'catalogo/:empresaId', name: 'catalogo', component: CatalogoPage, meta: { roles: [''] } },
      { path: 'notificaciones', name: 'notificaciones', component: NotificacionesPage },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: () => ruta_inicial(sesion.usuario.value?.rol),
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

router.beforeEach(async (to) => {
  const autenticado = sesion.autenticado.value || await restaurar_sesion();
  if (to.meta.publica) return autenticado ? ruta_inicial(sesion.usuario.value?.rol) : true;
  if (!autenticado) return '/login';
  const roles = to.meta.roles as string[] | undefined;
  if (roles && !roles.includes(sesion.usuario.value?.rol || '')) return ruta_inicial(sesion.usuario.value?.rol);
  return true;
});

export default router;
