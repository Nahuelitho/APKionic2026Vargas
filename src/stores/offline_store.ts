import { computed, ref, watch } from 'vue';
import { Network } from '@capacitor/network';
import { avisar } from '../services/aviso_service';
import { obtener_access_token, sesion } from '../services/auth_service';
import {
  cambiar_estado_pedido,
  type EstadoPedido,
  type Pedido,
} from '../services/pedidos_service';
import {
  borrar_cola,
  guardar_cola as guardar_cola_almacenada,
  leer_cola,
  type CambioPendiente,
} from '../services/offline_queue_service';

const en_linea = ref(typeof navigator === 'undefined' ? true : navigator.onLine);
const cola = ref<CambioPendiente[]>(leer_cola());
const sincronizando = ref(false);
// Se incrementa cuando la cola bajo: las pantallas recargan una sola vez.
const ultima_sincronizacion = ref(0);
let iniciado = false;

const sin_conexion = computed(() => !en_linea.value);

// Al cerrar la sesion la cola de memoria tambien se limpia; al entrar se relee.
watch(() => sesion.usuario.value?.id, id => {
  cola.value = id ? leer_cola() : [];
}, { flush: 'sync' });

function guardar_cola(nueva_cola: CambioPendiente[]) {
  cola.value = nueva_cola;
  guardar_cola_almacenada(nueva_cola);
}

function encolar(pedido_id: number, estado: EstadoPedido) {
  guardar_cola([
    ...cola.value,
    { id: `${Date.now()}-${Math.random().toString(36).slice(2)}`, pedido_id, estado, creado_en: new Date().toISOString() },
  ]);
}

function cambios_de_pedido(pedido_id: number) {
  return cola.value.filter(cambio => cambio.pedido_id === pedido_id);
}

// El cambio que espera en la cola se dibuja encima de lo que dice la API.
export function con_cambios_sin_enviar<T extends Pedido>(pedido: T): T {
  const cambios = cambios_de_pedido(pedido.id);
  if (cambios.length === 0) return pedido;
  // Vale el ultimo cambio encolado para ese pedido.
  return { ...pedido, estado: cambios[cambios.length - 1].estado, sin_enviar: true };
}

async function sincronizar() {
  // Sin sesion no se envia: la solicitud saldria sin token.
  if (sincronizando.value || cola.value.length === 0 || !obtener_access_token()) return;

  sincronizando.value = true;
  const en_cola = cola.value.length;
  const rechazados: string[] = [];

  for (const cambio of [...cola.value]) {
    try {
      await cambiar_estado_pedido(cambio.pedido_id, cambio.estado);
    } catch (error) {
      const estado_http = (error as { estado_http?: number })?.estado_http;
      // Sin red, sesion vencida o servidor caido: sigue en la cola.
      if (!estado_http || estado_http === 401 || estado_http >= 500) continue;
      // Un rechazo de la API no mejora reintentando: sale de la cola y se avisa.
      rechazados.push((error as { mensaje?: string })?.mensaje || 'La API rechazo el cambio.');
    }
    guardar_cola(cola.value.filter(item => item.id !== cambio.id));
  }

  sincronizando.value = false;

  if (rechazados.length > 0) {
    void avisar(`La API no acepto ${rechazados.length} de los cambios guardados: ${rechazados[0]}`, 'danger');
  }
  // Algo salio de la cola: la lista se recarga y muestra lo que de verdad quedo.
  if (cola.value.length < en_cola) ultima_sincronizacion.value += 1;
}

async function iniciar() {
  if (iniciado) return;
  iniciado = true;

  try {
    // El sistema avisa cada cambio de la red: wifi que se corta, datos que vuelven.
    void Network.addListener('networkStatusChange', estado => {
      en_linea.value = estado.connected;
      if (estado.connected) void sincronizar();
    });
    // El estado al arrancar: lo que quedo en la cola de la sesion anterior sale ahora.
    const estado = await Network.getStatus();
    en_linea.value = estado.connected;
    if (en_linea.value) void sincronizar();
  } catch (error) {
    // Fallback para navegador o si el plugin nativo no esta disponible.
    console.warn('[offline] No se pudo consultar @capacitor/network', error);
    en_linea.value = navigator.onLine;
    window.addEventListener('online', () => { en_linea.value = true; void sincronizar(); });
    window.addEventListener('offline', () => { en_linea.value = false; });
    if (en_linea.value) void sincronizar();
  }
}

function limpiar_cola() {
  cola.value = [];
  borrar_cola();
}

export function use_offline_store() {
  return {
    en_linea,
    cola,
    sincronizando,
    sin_conexion,
    ultima_sincronizacion,
    iniciar,
    guardar_cola,
    encolar,
    cambios_de_pedido,
    sincronizar,
    limpiar_cola,
  };
}
