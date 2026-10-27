import { ref, watch } from 'vue';
import { sesion } from '../services/auth_service';
import {
  marcar_leida as marcar_leida_api,
  marcar_todas_leidas as marcar_todas_leidas_api,
  obtener_no_leidas,
  obtener_notificaciones,
  type Notificacion,
} from '../services/notificaciones_service';

const INTERVALO_MS = 15_000;

const notificaciones = ref<Notificacion[]>([]);
const no_leidas = ref(0);
const cargando = ref(false);
const marcando = ref(false);
const error = ref<string | null>(null);

let intervalo: ReturnType<typeof setInterval> | null = null;
let contador_en_curso: Promise<void> | null = null;
let version = 0;

// Al cambiar de usuario (login, logout o cambio de ambito) se limpia la bandeja.
watch(() => sesion.usuario.value?.id, () => {
  ++version;
  notificaciones.value = [];
  no_leidas.value = 0;
  cargando.value = false;
  marcando.value = false;
  error.value = null;
}, { flush: 'sync' });

function mensaje_error(excepcion: unknown, por_defecto: string) {
  const ajax_error = excepcion as { mensaje?: string; message?: string };
  return ajax_error.mensaje || ajax_error.message || por_defecto;
}

async function actualizar_contador() {
  if (!sesion.autenticado.value || contador_en_curso) return;
  contador_en_curso = (async () => {
    try {
      const respuesta = await obtener_no_leidas();
      if (sesion.autenticado.value) no_leidas.value = respuesta.total;
    } catch (excepcion) {
      // El polling no interrumpe la pantalla: se reintenta en el proximo ciclo.
      console.warn('[notificaciones] No se pudo actualizar el contador', excepcion);
    } finally {
      contador_en_curso = null;
    }
  })();
  return contador_en_curso;
}

async function cargar_notificaciones() {
  if (!sesion.autenticado.value) return;
  const actual = ++version;
  cargando.value = true;
  error.value = null;
  try {
    const [lista, contador] = await Promise.all([obtener_notificaciones(), obtener_no_leidas()]);
    if (actual !== version) return;
    notificaciones.value = lista.notificaciones;
    no_leidas.value = contador.total;
  } catch (excepcion) {
    if (actual !== version) return;
    error.value = mensaje_error(excepcion, 'No se pudieron cargar las notificaciones.');
  } finally {
    if (actual === version) cargando.value = false;
  }
}

async function marcar_leida(id: number) {
  const notificacion = notificaciones.value.find(item => item.id === id);
  if (!notificacion || notificacion.leida || marcando.value) return false;
  marcando.value = true;
  error.value = null;
  try {
    await marcar_leida_api(id);
    notificacion.leida = true;
    no_leidas.value = Math.max(0, no_leidas.value - 1);
    return true;
  } catch (excepcion) {
    error.value = mensaje_error(excepcion, 'No se pudo marcar la notificacion como leida.');
    return false;
  } finally {
    marcando.value = false;
  }
}

async function marcar_todas() {
  if (marcando.value || no_leidas.value === 0) return false;
  marcando.value = true;
  error.value = null;
  try {
    await marcar_todas_leidas_api();
    notificaciones.value.forEach(notificacion => { notificacion.leida = true; });
    no_leidas.value = 0;
    return true;
  } catch (excepcion) {
    error.value = mensaje_error(excepcion, 'No se pudieron marcar las notificaciones como leidas.');
    return false;
  } finally {
    marcando.value = false;
  }
}

function iniciar_polling() {
  if (intervalo !== null) return;
  void actualizar_contador();
  intervalo = setInterval(() => { void actualizar_contador(); }, INTERVALO_MS);
}

function detener_polling() {
  if (intervalo === null) return;
  clearInterval(intervalo);
  intervalo = null;
}

export function use_notificaciones_store() {
  return {
    notificaciones,
    no_leidas,
    cargando,
    marcando,
    error,
    actualizar_contador,
    cargar_notificaciones,
    marcar_leida,
    marcar_todas,
    iniciar_polling,
    detener_polling,
  };
}
