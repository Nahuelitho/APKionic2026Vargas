/**
 * Tabla de estados del dominio.
 *
 * Cada estado tiene un unico texto, color e icono. Las pantallas NO
 * eligen colores a mano: piden el estado aca y comp-estado lo dibuja.
 */
import {
  checkmarkCircleOutline,
  closeCircleOutline,
  cloudUploadOutline,
  mailOpenOutline,
  mailUnreadOutline,
  timeOutline,
  helpCircleOutline,
} from 'ionicons/icons';

export type TonoEstado = 'exito' | 'aviso' | 'peligro' | 'neutral' | 'info';

export type DefinicionEstado = {
  texto: string;
  tono: TonoEstado;
  icono: string;
  descripcion: string;
};

export const ESTADOS_PEDIDO: Record<string, DefinicionEstado> = {
  'En preparacion': {
    texto: 'En preparación',
    tono: 'aviso',
    icono: timeOutline,
    descripcion: 'El pedido se está preparando.',
  },
  Listo: {
    texto: 'Listo',
    tono: 'exito',
    icono: checkmarkCircleOutline,
    descripcion: 'El pedido está listo para entregar.',
  },
  Cancelado: {
    texto: 'Cancelado',
    tono: 'peligro',
    icono: closeCircleOutline,
    descripcion: 'El pedido fue cancelado y ya no se modifica.',
  },
};

/** Marca local para una escritura que espera en la cola offline. */
export const ESTADO_SIN_ENVIAR: DefinicionEstado = {
  texto: 'Sin enviar',
  tono: 'aviso',
  icono: cloudUploadOutline,
  descripcion: 'El cambio se guardó en el teléfono y se enviará al volver la red.',
};

/** Activo / inactivo de empresas y productos. */
export const ESTADOS_ACTIVO: Record<'activo' | 'inactivo', DefinicionEstado> = {
  activo: {
    texto: 'Activa',
    tono: 'exito',
    icono: checkmarkCircleOutline,
    descripcion: 'Disponible en este momento.',
  },
  inactivo: {
    texto: 'Inactiva',
    tono: 'neutral',
    icono: closeCircleOutline,
    descripcion: 'No está disponible en este momento.',
  },
};

/** Estado de stock de un producto. */
export const ESTADOS_STOCK: Record<'disponible' | 'sin_stock', DefinicionEstado> = {
  disponible: {
    texto: 'Disponible',
    tono: 'exito',
    icono: checkmarkCircleOutline,
    descripcion: 'Se puede agregar al pedido.',
  },
  sin_stock: {
    texto: 'Sin stock',
    tono: 'peligro',
    icono: closeCircleOutline,
    descripcion: 'No se puede agregar hasta que vuelva el stock.',
  },
};

/** Estado de lectura de una notificacion. */
export const ESTADOS_NOTIFICACION: Record<'nueva' | 'leida', DefinicionEstado> = {
  nueva: {
    texto: 'Nueva',
    tono: 'info',
    icono: mailUnreadOutline,
    descripcion: 'Todavía no la abriste.',
  },
  leida: {
    texto: 'Leída',
    tono: 'neutral',
    icono: mailOpenOutline,
    descripcion: 'Ya la abriste.',
  },
};

export function definir_estado_pedido(estado: string | null | undefined): DefinicionEstado {
  return (estado && ESTADOS_PEDIDO[estado]) || {
    texto: estado || 'Sin estado',
    tono: 'neutral',
    icono: helpCircleOutline,
    descripcion: 'Estado no reconocido por la app.',
  };
}
