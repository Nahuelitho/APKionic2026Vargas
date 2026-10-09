import { ajax_binario, ajax_request } from './ajax_service';

export type EstadoPedido = 'En preparacion' | 'Listo' | 'Cancelado';
export type Pedido = { id: number; cliente: string; fecha: string; estado: EstadoPedido; total: number; empresa_id: number; usuario_id: number; nombre_empresa: string };
export type PedidoItem = {
  id: number; productoId: number | null; nombre: string;
  precioUnitario: number; cantidad: number; subtotal: number;
};
export type PedidoDetalle = Pedido & { items: PedidoItem[] };
export type PedidoRequest = { cliente: string; empresa_id: number; items: { productoId: number; cantidad: number }[] };

export function codigo_pedido(id: number) {
  if (!Number.isSafeInteger(id) || id <= 0) throw new Error('ID de pedido invalido.');
  return `PED-${String(id).padStart(4, '0')}`;
}

export function parsear_codigo_pedido(codigo: string): number {
  if (!/^PED-\d{4,16}$/.test(codigo)) throw new Error('Ingresa un codigo como PED-0007.');
  const id = Number(codigo.slice(4));
  if (!Number.isSafeInteger(id) || id <= 0 || codigo_pedido(id) !== codigo) {
    throw new Error('El codigo de pedido no es valido.');
  }
  return id;
}

export function obtener_pedidos() {
  return ajax_request<{ pedidos: Pedido[] }>('/api/pedidos');
}
export function obtener_pedido(id: number) {
  codigo_pedido(id);
  return ajax_request<PedidoDetalle>(`/api/pedidos/${id}`);
}
export function crear_pedido(datos: PedidoRequest) {
  return ajax_request<PedidoDetalle>('/api/pedidos', { method: 'POST', body: datos });
}
export function cambiar_estado_pedido(id: number, estado: EstadoPedido) {
  codigo_pedido(id);
  return ajax_request<PedidoDetalle>(`/api/pedidos/${id}/estado`, { method: 'PUT', body: { estado } });
}
export function obtener_qr_pedido(id: number): Promise<Blob> {
  codigo_pedido(id);
  return ajax_binario(`/api/pedidos/${id}/qr`);
}
export function obtener_comprobante_pedido(id: number): Promise<Blob> {
  codigo_pedido(id);
  return ajax_binario(`/api/pedidos/${id}/comprobante`);
}
