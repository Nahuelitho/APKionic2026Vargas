import { ajax_request } from './ajax_service';

/** Alta/baja del token FCM en la tabla de telefonos del backend. */
export function registrar_dispositivo(token: string, plataforma = 'android') {
  return ajax_request<void>('/api/dispositivos', {
    method: 'POST',
    body: { token, plataforma },
    silencioso: true,
  });
}

export function eliminar_dispositivo(token: string) {
  return ajax_request<void>(`/api/dispositivos/${encodeURIComponent(token)}`, {
    method: 'DELETE',
    silencioso: true,
  });
}

export function eliminar_dispositivos() {
  return ajax_request<void>('/api/dispositivos', { method: 'DELETE', silencioso: true });
}
