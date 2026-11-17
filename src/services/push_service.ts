import { Capacitor } from '@capacitor/core';
import { LocalNotifications } from '@capacitor/local-notifications';
import { SecureStorage } from '@aparajita/capacitor-secure-storage';
import { PushNotifications, type ActionPerformed, type PushNotificationSchema } from '@capacitor/push-notifications';
import type { Router } from 'vue-router';
import { sesion } from './auth_service';
import { eliminar_dispositivo, registrar_dispositivo } from './dispositivos_service';
import { use_notificaciones_store } from '../stores/notificaciones_store';

/**
 * Push FCM + navegacion profunda.
 *
 * La push de un cambio de pedido trae `pedido_id` en `data`. Al tocarla,
 * la app se abre en /app/pedidos?pedido=ID. Si la app estaba cerrada,
 * la navegacion queda pendiente hasta que el router y la sesion esten
 * listos (ver App.vue).
 */

const CLAVE_TOKEN_PUSH = 'vargas_push_token';
const CANAL_ANDROID = 'vargas_importantes';

let inicializado = false;
let token: string | null = null;
let router: Router | null = null;
let ruta_pendiente: string | null = null;
// Evita que el login pise con su ruta inicial la navegacion que disparo una push.
let navegacion_push_en_curso = false;

function nativo() {
  return Capacitor.isNativePlatform() && Capacitor.isPluginAvailable('PushNotifications');
}

async function leer_token() {
  if (token) return token;
  try {
    const guardado = await SecureStorage.get(CLAVE_TOKEN_PUSH);
    token = typeof guardado === 'string' ? guardado : null;
  } catch {
    token = localStorage.getItem(CLAVE_TOKEN_PUSH);
  }
  return token;
}

async function guardar_token(valor: string) {
  token = valor;
  try { await SecureStorage.set(CLAVE_TOKEN_PUSH, valor); } catch { localStorage.setItem(CLAVE_TOKEN_PUSH, valor); }
}

async function borrar_token() {
  token = null;
  try { await SecureStorage.remove(CLAVE_TOKEN_PUSH); } catch { localStorage.removeItem(CLAVE_TOKEN_PUSH); }
}

function datos_de_push(notificacion: PushNotificationSchema | ActionPerformed['notification']) {
  return (notificacion.data || {}) as Record<string, unknown>;
}

/** El identificador viaja como texto en data porque FCM no admite anidados. */
export function ruta_de_datos(datos: Record<string, unknown>): string | null {
  const pedido = datos['pedido_id'] ?? datos['pedidoId'];
  const id = Number(pedido);
  if (Number.isSafeInteger(id) && id > 0) return `/app/pedidos?pedido=${id}`;
  return null;
}

function navegar(destino: string | null) {
  if (!destino) return;
  // Sin sesion no se navega: el pedido volveria al login y se perderia el toque.
  if (!sesion.autenticado.value || !router) { ruta_pendiente = destino; return; }
  ruta_pendiente = null;
  navegacion_push_en_curso = true;
  void router.push(destino);
}

/** Lo llama App.vue cuando el router y la sesion ya estan listos. */
export function intentar_navegacion_pendiente() {
  if (ruta_pendiente) navegar(ruta_pendiente);
}

/**
 * El login lo consulta antes de ir a la ruta inicial: si esta sesion se
 * abrio tocando una push, primero debe ganar la pantalla del pedido.
 */
export function hubo_navegacion_push() {
  return navegacion_push_en_curso;
}

export function conectar_navegacion(router_actual: Router) {
  router = router_actual;
  intentar_navegacion_pendiente();
}

/** Manda al backend el token que Firebase entrego (al entrar o al renovarlo). */
export async function registrar_sesion_push() {
  const valor = await leer_token();
  if (!valor) {
    // Despues de Salir el telefono quedo desregistrado: al volver a entrar se registra de nuevo.
    if (inicializado && nativo()) {
      try { await PushNotifications.register(); } catch (error) {
        console.warn('[push] No se pudo volver a registrar en FCM', error);
      }
    }
    return;
  }
  if (!sesion.autenticado.value) return;
  try { await registrar_dispositivo(valor, 'android'); } catch (error) {
    console.warn('[push] No se pudo registrar el telefono en la API', error);
  }
}

/**
 * Baja al salir: se borra el telefono en la API y se desregistra de FCM,
 * asi no llega nada mas aunque el telefono siga instalado y sin red.
 */
export async function dar_de_baja_push() {
  const valor = await leer_token();
  if (valor) {
    try { await eliminar_dispositivo(valor); } catch (error) {
      console.warn('[push] No se pudo dar de baja el telefono en la API', error);
    }
  }
  await borrar_token();
  navegacion_push_en_curso = false;
  if (!nativo()) return;
  try { await PushNotifications.unregister(); }
  catch (error) { console.warn('[push] No se pudo desregistrar de FCM', error); }
}

export async function preparar_push() {
  if (inicializado) return;
  if (import.meta.env.VITE_PUSH_NOTIFICATIONS_ENABLED === 'false') return;
  if (!nativo()) return;

  try {
    const permiso = await PushNotifications.requestPermissions();
    if (permiso.receive !== 'granted') {
      console.warn('[push] Sin permiso para recibir push notifications');
      return;
    }

    // Los listeners van ANTES de register(): si el token llega rapido, no se pierde.
    await PushNotifications.addListener('registration', async (recibido) => {
      await guardar_token(recibido.value);
      console.info('[push] Token FCM guardado:', recibido.value.slice(0, 18) + '...');
      await registrar_sesion_push();
    });

    await PushNotifications.addListener('registrationError', (error) => {
      console.warn('[push] Error al registrar en FCM:', error);
    });

    // App abierta: FCM no dibuja la push solo, se muestra una local.
    await PushNotifications.addListener('pushNotificationReceived', (notificacion) => {
      if (!sesion.autenticado.value) return;
      void use_notificaciones_store().actualizar_contador();
      void import('./notificaciones_nativas_service').then(({ notificar_local }) =>
        notificar_local(notificacion.title || 'Novedad', notificacion.body || '', notificacion.id, {
          ...datos_de_push(notificacion),
          ruta: ruta_de_datos(datos_de_push(notificacion)) || undefined,
        }),
      );
    });

    // Toque en la push: abre el dato que cambio.
    await PushNotifications.addListener('pushNotificationActionPerformed', (accion) => {
      const datos = datos_de_push(accion.notification);
      navegar(ruta_de_datos(datos) || (typeof datos['ruta'] === 'string' ? datos['ruta'] : null));
    });

    // Toque en la notificacion local (app abierta).
    await LocalNotifications.addListener('localNotificationActionPerformed', (accion) => {
      const datos = (accion.notification.extra || {}) as Record<string, unknown>;
      navegar(ruta_de_datos(datos) || (typeof datos['ruta'] === 'string' ? datos['ruta'] : null));
    });

    inicializado = true;
    console.log('[push] Push inicializado; canal', CANAL_ANDROID);
    await PushNotifications.register();
  } catch (error) {
    console.warn('[push] No se pudo inicializar PushNotifications', error);
  }
}

export async function reiniciar_push_para_sesion() {
  if (!nativo()) return;
  await registrar_sesion_push();
}
