import { ajax_request } from './ajax_service';

export type Notificacion = {
  id: number;
  titulo: string;
  mensaje: string;
  pedido_id: number | null;
  leida: boolean;
  fecha: string;
};

export function obtener_notificaciones() {
  return ajax_request<{ notificaciones: Notificacion[] }>('/api/notificaciones', { guardable: true });
}

export function obtener_no_leidas() {
  // silencioso: lo llama el polling de la campana; no debe vibrar si la API no responde.
  return ajax_request<{ total: number }>('/api/notificaciones/no-leidas', { silencioso: true });
}

export function marcar_leida(id: number) {
  return ajax_request<void>(`/api/notificaciones/${id}/leida`, { method: 'PUT' });
}

export function marcar_todas_leidas() {
  return ajax_request<void>('/api/notificaciones/leidas', { method: 'PUT' });
}
