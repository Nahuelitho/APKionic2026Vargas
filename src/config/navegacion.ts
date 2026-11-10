import type { Component } from 'vue';

import InicioPage from '../views/InicioPage.vue';
import ProductosPage from '../views/ProductosPage.vue';
import ClientesPage from '../views/ClientesPage.vue';
import PedidosPage from '../views/PedidosPage.vue';
import MiCuentaPage from '../views/MiCuentaPage.vue';
import UsuariosPage from '../views/UsuariosPage.vue';
import EmpresasPage from '../views/EmpresasPage.vue';
import MiEmpresaPage from '../views/MiEmpresaPage.vue';
import ComprarPage from '../views/ComprarPage.vue';

import {
  homeOutline,
  pricetagOutline,
  peopleOutline,
  receiptOutline,
  personCircleOutline,
  personAddOutline,
  storefrontOutline,
} from 'ionicons/icons';

export type ItemNavegacion = {
  id: string;
  titulo: string;
  ruta: string;
  icono: string;
  orden: number;
  grupo: 'principal' | 'configuracion';
  tab: boolean;
  componente: Component;
  roles?: string[];
  tab_roles?: string[];
  // La pantalla tiene sus lecturas guardables y se puede usar sin conexion.
  sin_red?: boolean;
};

export const navegacion: ItemNavegacion[] = [
  {
    id: 'comprar', titulo: 'Tiendas', ruta: '/app/comprar', icono: storefrontOutline,
    orden: 15, grupo: 'principal', tab: true, componente: ComprarPage, roles: [''], sin_red: true,
  },
  {
    id: 'empresas',
    titulo: 'Empresas',
    ruta: '/app/empresas',
    icono: storefrontOutline,
    orden: 44,
    grupo: 'configuracion',
    tab: false,
    componente: EmpresasPage,
    // Admin y superadmin entran a la lista; desde ahí abren el detalle de cada empresa.
    roles: ['superadmin', 'administrador'],
  },
  {
    id: 'mi-empresa',
    titulo: 'Mi Empresa',
    ruta: '/app/mi-empresa',
    icono: storefrontOutline,
    orden: 43,
    grupo: 'configuracion',
    tab: false,
    componente: MiEmpresaPage,
    roles: ['vendedor'],
  },
  {
    id: 'inicio',
    titulo: 'Inicio',
    ruta: '/app/inicio',
    icono: homeOutline,
    orden: 10,
    grupo: 'principal',
    tab: true,
    componente: InicioPage,
    roles: ['superadmin', 'administrador', 'vendedor', 'caja'],
    sin_red: true,
  },
  {
    id: 'productos',
    titulo: 'Productos',
    ruta: '/app/productos',
    icono: pricetagOutline,
    orden: 20,
    grupo: 'principal',
    tab: true,
    componente: ProductosPage,
    roles: ['superadmin', 'administrador', 'vendedor'],
    sin_red: true,
  },
  {
    id: 'clientes',
    titulo: 'Clientes',
    ruta: '/app/clientes',
    icono: peopleOutline,
    orden: 30,
    grupo: 'principal',
    tab: true,
    componente: ClientesPage,
    roles: ['superadmin', 'administrador', 'vendedor'],
    tab_roles: ['superadmin', 'administrador', 'vendedor'],
    sin_red: true,
  },
  {
    id: 'pedidos',
    titulo: 'Pedidos',
    ruta: '/app/pedidos',
    icono: receiptOutline,
    orden: 40,
    grupo: 'principal',
    tab: true,
    componente: PedidosPage,
    tab_roles: ['superadmin', 'administrador', 'vendedor', 'caja', ''],
    sin_red: true,
  },
  {
    id: 'usuarios',
    titulo: 'Usuarios',
    ruta: '/app/usuarios',
    icono: personAddOutline,
    orden: 45,
    grupo: 'configuracion',
    tab: false,
    componente: UsuariosPage,
    roles: ['superadmin', 'administrador'],
    tab_roles: [],
  },
  {
    id: 'mi-cuenta',
    titulo: 'Mi cuenta',
    ruta: '/app/mi-cuenta',
    icono: personCircleOutline,
    orden: 50,
    grupo: 'configuracion',
    tab: true,
    componente: MiCuentaPage,
    sin_red: true,
  },
];

export const navegacion_ordenada = [...navegacion].sort((a, b) => a.orden - b.orden);

export function ruta_inicial(rol: string | null | undefined): string {
  if (!rol) return '/app/comprar';
  if (rol === 'superadmin') return '/app/empresas';
  if (rol === 'caja') return '/app/pedidos';
  return '/app/inicio';
}

export function puede_ver_item(
  item: ItemNavegacion,
  rol: string | null | undefined,
  sin_conexion = false,
): boolean {
  // Sin red primero se descartan las pantallas que no tienen datos guardados.
  if (sin_conexion && !item.sin_red) return false;
  return !item.roles || item.roles.includes(rol || '');
}

export function titulo_item(item: ItemNavegacion, rol: string | null | undefined): string {
  return item.id === 'pedidos' && !rol ? 'Mis pedidos' : item.titulo;
}
