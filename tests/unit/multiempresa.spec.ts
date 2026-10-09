import { mount, flushPromises } from '@vue/test-utils';
import { describe, expect, test, vi, beforeEach } from 'vitest';
import { sesion } from '../../src/services/auth_service';
import { obtener_productos, crear_producto, obtener_foto_producto } from '../../src/services/productos_service';
import { crear_pedido } from '../../src/services/pedidos_service';
import { ajax_binario, ajax_request } from '../../src/services/ajax_service';
import PedidosPage from '../../src/views/PedidosPage.vue';
import ProductosPage from '../../src/views/ProductosPage.vue';
import { navegacion } from '../../src/config/navegacion';

vi.mock('../../src/services/ajax_service', () => ({ ajax_request: vi.fn(), ajax_binario: vi.fn() }));
vi.mock('../../src/services/empresas_service', () => ({ obtener_empresas: vi.fn(async () => [
  { id: 1, nombre_empresa: 'Vargas', activo: true }, { id: 2, nombre_empresa: 'Segunda', activo: true },
]) }));
vi.mock('@ionic/vue', async () => {
  const actual = await vi.importActual<any>('@ionic/vue');
  const { onMounted } = await import('vue');
  return { ...actual, onIonViewWillEnter: onMounted, onIonViewWillLeave: vi.fn(), onIonViewDidLeave: vi.fn() };
});

const producto = (id: number, empresa_id: number, nombre_empresa: string) => ({ id, empresa_id, nombre_empresa,
  nombre: `Producto ${id}`, descripcion: null, precio: 10, stock: true, fotoUrl: null });
const pedido = { id: 1, empresa_id: 1, usuario_id: 3, nombre_empresa: 'Vargas', cliente: 'Ana',
  fecha: '2026-10-08T12:00:00Z', estado: 'Listo', total: 10, items: [] };
const global = { renderStubDefaultSlot: true, stubs: {
  CompPage: { props: ['titulo'], template: '<main><h1>{{ titulo }}</h1><slot /></main>' },
  CompEsqueleto: true,
  ...Object.fromEntries(['IonBadge', 'IonButton', 'IonCard', 'IonCardContent', 'IonCardHeader', 'IonCardTitle',
    'IonInput', 'IonItem', 'IonLabel', 'IonList', 'IonSelect', 'IonSelectOption', 'IonText', 'IonTextarea',
    'IonToggle', 'IonThumbnail', 'IonItemDivider'].map(name => [name, true])),
} };

beforeEach(() => {
  vi.clearAllMocks();
  sesion.usuario.value = { id: 3, nombre: 'Comun', email: 'comun@test.com', rol: null, empresa_id: null };
  vi.mocked(ajax_request).mockImplementation(async (ruta: string) => {
    if (ruta === '/api/pedidos') return { pedidos: [pedido] };
    if (ruta === '/api/pedidos/1') return pedido;
    const empresa = new URL(ruta, 'http://tests').searchParams.get('empresa_id');
    return { productos: [producto(101, 1, 'Vargas'), producto(201, 2, 'Segunda')].filter(p => !empresa || p.empresa_id === Number(empresa)), pagina: 1, tamanio: 10, total: 12, total_paginas: 2 };
  });
});

describe('Multiempresa', () => {
  test('el catalogo filtra por empresa y conserva el contrato multipart', async () => {
    await obtener_productos(2, 10, 7);
    expect(ajax_request).toHaveBeenCalledWith('/api/productos?pagina=2&tamanio=10&empresa_id=7');
    await crear_producto({ nombre: 'Producto', descripcion: null, precio: 10, stock: true, empresa_id: 7 });
    const datos = vi.mocked(ajax_request).mock.calls[1][1]?.body as FormData;
    expect(datos.get('empresaId')).toBe('7');
    await obtener_foto_producto('/uploads/productos/123.png');
    expect(ajax_binario).toHaveBeenCalledWith('/uploads/productos/123.png');
    await crear_pedido({ cliente: 'Ana', empresa_id: 7, items: [{ productoId: 101, cantidad: 2 }] });
    expect(ajax_request).toHaveBeenLastCalledWith('/api/pedidos', { method: 'POST', body: {
      cliente: 'Ana', empresa_id: 7, items: [{ productoId: 101, cantidad: 2 }],
    } });
  });

  test('el comun ve mis pedidos y seleccion de empresa, sin PDF ni cambios de estado', async () => {
    const wrapper = mount(PedidosPage, { global }); await flushPromises();
    expect(wrapper.text()).toContain('Mis pedidos');
    expect(wrapper.findAll('ion-select-option-stub').map(x => x.text())).toEqual(['Vargas', 'Segunda']);
    const fila = wrapper.findAll('ion-item-stub').find(x => x.text().includes('PED-0001'))!;
    await fila.trigger('click'); await flushPromises();
    expect(wrapper.text()).toContain('Ver QR');
    expect(wrapper.text()).not.toContain('Descargar PDF');
    expect(wrapper.text()).not.toContain('Marcar Listo');
    wrapper.unmount();
  });

  test('catalogo comun agrupado y paginacion disponible sin acciones de gestion', async () => {
    const wrapper = mount(ProductosPage, { global }); await flushPromises();
    expect(wrapper.findAll('ion-item-divider-stub').map(x => x.text())).toEqual(['Vargas', 'Segunda']);
    expect(wrapper.text()).toContain('Ver mas');
    expect(wrapper.text()).not.toContain('Editar');
    expect(wrapper.text()).not.toContain('Borrar');
    wrapper.unmount();
  });

  test('cambiar empresa filtra el catalogo y elimina productos de la seleccion anterior', async () => {
    const wrapper = mount(PedidosPage, { global }); await flushPromises();
    await wrapper.findAll('ion-button-stub').find(x => x.text() === 'Agregar')!.trigger('click');
    expect(wrapper.text()).toContain('por unidad');
    const selector = wrapper.findComponent('ion-select-stub');
    selector.vm.$emit('update:modelValue', 2);
    selector.vm.$emit('ionChange', { detail: { value: 2 } });
    await flushPromises();
    expect(wrapper.text()).not.toContain('por unidad');
    expect(ajax_request).toHaveBeenCalledWith('/api/productos?pagina=1&tamanio=10&empresa_id=2');
    wrapper.unmount();
  });

  test('superadmin puede acceder a empresas y usuarios; comun no tiene esas rutas', () => {
    expect(navegacion.find(x => x.id === 'empresas')?.roles).toEqual(['superadmin']);
    expect(navegacion.find(x => x.id === 'usuarios')?.roles).toEqual(['superadmin', 'administrador']);
    expect(navegacion.find(x => x.id === 'pedidos')?.roles).toBeUndefined();
  });
});
