import type { Component } from 'vue';

import InicioPage from '../views/InicioPage.vue';
import ProductosPage from '../views/ProductosPage.vue';
import ClientesPage from '../views/ClientesPage.vue';
import PedidosPage from '../views/PedidosPage.vue';
import MiCuentaPage from '../views/MiCuentaPage.vue';
import UsuariosPage from '../views/UsuariosPage.vue';

import {
  homeOutline,
  pricetagOutline,
  peopleOutline,
  receiptOutline,
  personCircleOutline,
  personAddOutline,
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
};

export const navegacion: ItemNavegacion[] = [
  {
    id: 'inicio',
    titulo: 'Inicio',
    ruta: '/app/inicio',
    icono: homeOutline,
    orden: 10,
    grupo: 'principal',
    tab: true,
    componente: InicioPage,
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
    roles: ['administrador', 'vendedor'],
    tab_roles: ['administrador', 'vendedor'],
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
    roles: ['administrador', 'vendedor'],
    tab_roles: ['administrador', 'vendedor'],
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
    roles: ['administrador', 'vendedor', 'caja'],
    tab_roles: ['administrador', 'vendedor', 'caja'],
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
    roles: ['administrador'],
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
  },
];

export const navegacion_ordenada = [...navegacion].sort((a, b) => a.orden - b.orden);
