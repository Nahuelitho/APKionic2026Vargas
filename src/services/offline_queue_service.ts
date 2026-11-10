import type { EstadoPedido } from './pedidos_service';

// La cola vive en localStorage: el cambio que espera sobrevive al cierre de la app.
const clave_cola = 'multitenant_vargas_cola_offline';

export type CambioPendiente = {
  id: string;
  pedido_id: number;
  estado: EstadoPedido;
  creado_en: string;
};

export function leer_cola(): CambioPendiente[] {
  try {
    const guardada = JSON.parse(localStorage.getItem(clave_cola) || '[]');
    return Array.isArray(guardada) ? guardada : [];
  } catch {
    return [];
  }
}

export function guardar_cola(nueva_cola: CambioPendiente[]) {
  localStorage.setItem(clave_cola, JSON.stringify(nueva_cola));
}

export function borrar_cola() {
  localStorage.removeItem(clave_cola);
}
