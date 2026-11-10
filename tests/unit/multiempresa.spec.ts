import { mount, flushPromises } from '@vue/test-utils';
import { describe, expect, test, vi, beforeEach } from 'vitest';
import { sesion } from '../../src/services/auth_service';
import { obtener_productos, crear_producto, obtener_foto_producto } from '../../src/services/productos_service';
import { crear_pedido } from '../../src/services/pedidos_service';
import { ajax_binario, ajax_request } from '../../src/services/ajax_service';
import PedidosPage from '../../src/views/PedidosPage.vue';
import ProductosPage from '../../src/views/ProductosPage.vue';
import ProductosEmpresaPage from '../../src/views/ProductosEmpresaPage.vue';
import ComprarPage from '../../src/views/ComprarPage.vue';
import CatalogoPage from '../../src/views/CatalogoPage.vue';
import CompPedidoFormulario from '../../src/components/comp-pedido-formulario.vue';
import { obtener_empresas } from '../../src/services/empresas_service';
import { navegacion, puede_ver_item, ruta_inicial, titulo_item } from '../../src/config/navegacion';

const rutas = vi.hoisted(() => ({ route: { params: { empresaId: '2' }, query: {} as Record<string, string> }, push: vi.fn() }));
vi.mock('vue-router', async () => ({
  ...await vi.importActual<any>('vue-router'),
  useRoute: () => rutas.route,
  useRouter: () => ({ push: rutas.push }),
  onBeforeRouteLeave: vi.fn(), onBeforeRouteUpdate: vi.fn(),
}));

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
  rutas.route.params.empresaId = '2'; rutas.route.query = {};
  vi.mocked(obtener_empresas).mockResolvedValue([
    { id: 1, nombre_empresa: 'Vargas', activo: true }, { id: 2, nombre_empresa: 'Segunda', activo: true },
  ]);
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
    expect(ajax_request).toHaveBeenCalledWith('/api/productos?pagina=2&tamanio=10&empresa_id=7', { guardable: true });
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

  test('el comun ve solo historial y detalle, sin formulario, PDF ni cambios de estado', async () => {
    const wrapper = mount(PedidosPage, { global }); await flushPromises();
    expect(wrapper.text()).toContain('Mis pedidos');
    expect(wrapper.text()).toContain('Elegir empresa');
    expect(wrapper.findComponent(CompPedidoFormulario).exists()).toBe(false);
    const fila = wrapper.findAll('ion-item-stub').find(x => x.text().includes('PED-0001'))!;
    await fila.trigger('click'); await flushPromises();
    expect(wrapper.text()).toContain('Ver QR');
    expect(wrapper.text()).not.toContain('Descargar PDF');
    expect(wrapper.text()).not.toContain('Marcar Listo');
    wrapper.unmount();
  });

  test('el comprador abre el catalogo de la empresa elegida, sin acciones de gestion', async () => {
    const wrapper = mount(CatalogoPage, { global }); await flushPromises();
    expect(wrapper.text()).toContain('Segunda');
    expect(wrapper.text()).toContain('Producto 201');
    expect(wrapper.text()).not.toContain('Producto 101');
    expect(wrapper.text()).toContain('Siguiente');
    expect(wrapper.text()).not.toContain('Editar');
    expect(wrapper.text()).not.toContain('Borrar');
    await wrapper.findAll('ion-button-stub').find(x => x.text() === 'Siguiente')!.trigger('click');
    await flushPromises();
    expect(ajax_request).toHaveBeenCalledWith('/api/productos?pagina=2&tamanio=10&empresa_id=2', { guardable: true });
    wrapper.unmount();
  });

  test('cambiar empresa filtra el catalogo y elimina productos de la seleccion anterior', async () => {
    sesion.usuario.value = { id: 1, nombre: 'Admin', email: 'admin@test.com', rol: 'superadmin', empresa_id: null };
    const confirmar = vi.spyOn(window, 'confirm').mockReturnValue(false);
    const wrapper = mount(CompPedidoFormulario, { global }); await flushPromises();
    await wrapper.findAll('ion-button-stub').find(x => x.text() === 'Agregar')!.trigger('click');
    expect(wrapper.text()).toContain('por unidad');
    const selector = wrapper.findComponent('ion-select-stub');
    selector.vm.$emit('ionChange', { detail: { value: 2 } });
    await flushPromises();
    expect(wrapper.text()).toContain('por unidad');
    expect(ajax_request).not.toHaveBeenCalledWith('/api/productos?pagina=1&tamanio=10&empresa_id=2', { guardable: true });
    confirmar.mockReturnValue(true);
    selector.vm.$emit('ionChange', { detail: { value: 2 } });
    await flushPromises();
    expect(wrapper.text()).not.toContain('por unidad');
    expect(ajax_request).toHaveBeenCalledWith('/api/productos?pagina=1&tamanio=10&empresa_id=2', { guardable: true });
    wrapper.unmount();
    confirmar.mockRestore();
  });

  test('elegir empresa muestra solo activas sin controles administrativos', async () => {
    vi.mocked(obtener_empresas).mockResolvedValue([
      { id: 1, nombre_empresa: 'Vargas', activo: true }, { id: 2, nombre_empresa: 'Cerrada', activo: false },
    ]);
    const wrapper = mount(ComprarPage, { global }); await flushPromises();
    expect(wrapper.text()).toContain('Vargas');
    expect(wrapper.text()).not.toContain('Cerrada');
    expect(wrapper.text()).not.toContain('Guardar');
    const boton_catalogo = wrapper.findAllComponents('ion-button-stub').find(x => x.props('routerLink') === '/app/catalogo/1');
    expect(boton_catalogo).toBeDefined();
    wrapper.unmount();
  });

  test('elegir empresa solo habilita el mapa cuando la empresa tiene coordenadas', async () => {
    vi.mocked(obtener_empresas).mockResolvedValue([
      { id: 1, nombre_empresa: 'Vargas', activo: true, latitud: -31.4, longitud: -64.1 },
      { id: 2, nombre_empresa: 'Sin punto', activo: true, latitud: null, longitud: null },
    ]);
    const wrapper = mount(ComprarPage, { global }); await flushPromises();
    const botones_mapa = wrapper.findAllComponents('ion-button-stub').filter(x => x.attributes('data-test') === 'mapa');
    expect(botones_mapa).toHaveLength(2);
    expect(botones_mapa[0].props('disabled')).toBe(false);
    expect(botones_mapa[1].props('disabled')).toBe(true);
    wrapper.unmount();
  });

  test('una empresa inexistente no carga productos ni permite crear un pedido', async () => {
    rutas.route.params.empresaId = '99';
    const wrapper = mount(CatalogoPage, { global }); await flushPromises();
    expect(wrapper.text()).toContain('La empresa no existe o no esta activa');
    expect(wrapper.findComponent(CompPedidoFormulario).exists()).toBe(false);
    expect(ajax_request).not.toHaveBeenCalled();
    wrapper.unmount();
  });

  test('comprar confirma el pedido y abre su detalle en Mis pedidos', async () => {
    vi.mocked(ajax_request).mockImplementation(async (ruta, opciones) => {
      if (opciones?.method === 'POST') return { ...pedido, id: 8 };
      return { productos: [producto(201, 2, 'Segunda')], pagina: 1, total_paginas: 1 };
    });
    const wrapper = mount(CatalogoPage, { global }); await flushPromises();
    wrapper.findComponent('ion-input-stub').vm.$emit('update:modelValue', 'Ana');
    await wrapper.findAll('ion-button-stub').find(x => x.text() === 'Agregar')!.trigger('click');
    await wrapper.findAll('ion-button-stub').find(x => x.text() === 'Confirmar pedido')!.trigger('click');
    await flushPromises();
    expect(ajax_request).toHaveBeenCalledWith('/api/pedidos', { method: 'POST', body: {
      cliente: 'Ana', empresa_id: 2, items: [{ productoId: 201, cantidad: 1 }],
    } });
    expect(rutas.push).toHaveBeenCalledWith({ path: '/app/pedidos', query: { pedido: '8' } });
    wrapper.unmount();
  });

  test('Mis pedidos abre el detalle indicado al finalizar la compra', async () => {
    rutas.route.query = { pedido: '1' };
    const wrapper = mount(PedidosPage, { global }); await flushPromises();
    expect(wrapper.text()).toContain('Ver QR');
    expect(ajax_request).toHaveBeenCalledWith('/api/pedidos/1', { guardable: true });
    wrapper.unmount();
  });

  test('productos muestra el selector de empresas y navega a los productos de la empresa', async () => {
    const wrapper = mount(ProductosPage, { global }); await flushPromises();
    expect(wrapper.text()).toContain('Elegí una empresa');
    const fila = wrapper.findAll('ion-item-stub').find(x => x.text().includes('Vargas'))!;
    await fila.trigger('click');
    expect(rutas.push).toHaveBeenCalledWith('/app/empresas/1/productos');
    wrapper.unmount();
  });

  test('personal conserva la gestion de productos en la vista de la empresa', async () => {
    sesion.usuario.value = { id: 1, nombre: 'Admin', email: 'admin@test.com', rol: 'administrador', empresa_id: 2 };
    const wrapper = mount(ProductosEmpresaPage, { global }); await flushPromises();
    expect(wrapper.text()).toContain('Nuevo producto');
    expect(wrapper.text()).toContain('Editar');
    wrapper.unmount();
  });

  test('admin y superadmin administran empresas; el vendedor solo ve Mi Empresa', () => {
    expect(navegacion.find(x => x.id === 'empresas')?.roles).toEqual(['superadmin', 'administrador']);
    expect(navegacion.find(x => x.id === 'mi-empresa')?.roles).toEqual(['vendedor']);
    expect(navegacion.find(x => x.id === 'usuarios')?.roles).toEqual(['superadmin', 'administrador']);
    expect(navegacion.find(x => x.id === 'pedidos')?.roles).toBeUndefined();
  });

  test('entrada y menu corresponden al perfil sin otorgar una membresia al comprador', () => {
    expect(ruta_inicial(null)).toBe('/app/comprar');
    expect(ruta_inicial('superadmin')).toBe('/app/empresas');
    expect(ruta_inicial('caja')).toBe('/app/pedidos');
    expect(ruta_inicial('vendedor')).toBe('/app/inicio');
    expect(navegacion.filter(x => puede_ver_item(x, null)).map(x => x.id)).toEqual(['comprar', 'pedidos', 'mi-cuenta']);
    expect(titulo_item(navegacion.find(x => x.id === 'pedidos')!, null)).toBe('Mis pedidos');
    expect(puede_ver_item(navegacion.find(x => x.id === 'productos')!, 'caja')).toBe(false);
  });
});
