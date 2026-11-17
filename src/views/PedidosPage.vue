<template>
  <comp-page
    :titulo="sesion.usuario.value?.rol ? 'Pedidos' : 'Mis pedidos'"
    :mostrar_actualizar="true"
    :aviso_offline="sesion.usuario.value?.rol
      ? 'Estás viendo los pedidos guardados. Podés cambiar el estado; el cambio se envía solo cuando vuelva la red.'
      : 'Estás viendo tus pedidos guardados. Cuando vuelva la red se actualizan solos.'"
    @actualizar="actualizar"
  >
    <header class="encabezado">
      <div>
        <h1>{{ sesion.usuario.value?.rol ? 'Pedidos' : 'Mis pedidos' }}</h1>
        <p class="app-texto-suave">{{ resumen }}</p>
      </div>
      <ion-button v-if="sesion.usuario.value && !sesion.usuario.value.rol" fill="outline" router-link="/app/comprar">
        Elegir empresa
      </ion-button>
    </header>

    <!-- Buscador por codigo: presente solo con red porque el QR y el pedido viven en la API. -->
    <section v-if="!offline.sin_conexion.value" class="buscador app-tarjeta">
      <ion-input
        v-model="codigo"
        label="Código de pedido"
        label-placement="stacked"
        placeholder="PED-0007"
        helper-text="El código figura en el comprobante y en la notificación."
        @keyup.enter="abrir_codigo"
      />
      <div class="app-acciones">
        <ion-button fill="outline" :disabled="!codigo.trim()" @click="abrir_codigo">Abrir código</ion-button>
        <ion-button v-if="es_nativo" fill="clear" :disabled="escaneando" @click="escanear">
          <ion-icon slot="start" :icon="qrCodeOutline" />
          {{ escaneando ? 'Preparando escáner...' : 'Escanear QR' }}
        </ion-button>
      </div>
      <comp-aviso v-if="error_codigo" tipo="peligro" :mensaje="error_codigo" />
    </section>

    <comp-pedido-formulario v-if="activo && puede_crear && !offline.sin_conexion.value" @creado="pedido_creado" />

    <comp-lista
      :cargando="cargando"
      :error="error_lista"
      :items="pedidos.length"
      :texto_vacio="sesion.usuario.value?.rol
        ? 'No hay pedidos cargados todavía. Cuando se cree uno aparece acá.'
        : 'Todavía no hiciste pedidos. Elegí una tienda para empezar.'"
      :filas_esqueleto="6"
      @reintentar="cargar"
    >
      <ion-list class="lista-pedidos">
        <ion-item
          v-for="pedido in pedidos"
          :key="pedido.id"
          button
          detail
          class="pedido"
          @click="abrir_detalle(pedido.id)"
        >
          <ion-label class="ion-text-wrap">
            <div class="pedido-titulo">
              <h2>{{ codigo_pedido(pedido.id) }}</h2>
              <comp-estado v-if="pedido.sin_enviar" :sin_enviar="true" />
              <comp-estado v-else :estado="pedido.estado" />
            </div>
            <p>{{ pedido.cliente }} · {{ pedido.nombre_empresa }}</p>
            <p class="pedido-fecha">{{ fecha(pedido.fecha) }}</p>
            <p class="pedido-total">{{ dinero(pedido.total) }}</p>
          </ion-label>
        </ion-item>
      </ion-list>
    </comp-lista>

    <!-- Hoja de detalle: se abre desde abajo y se cierra con el gesto o el boton. -->
    <comp-detalle-hoja
      :abierto="detalle_id !== null"
      :titulo="detalle_id ? codigo_pedido(detalle_id) : 'Detalle del pedido'"
      :cargando="cargando_detalle"
      @cerrar="cerrar_detalle"
    >
      <comp-aviso
        v-if="error_detalle"
        tipo="peligro"
        titulo="No se pudo cargar el detalle"
        :mensaje="error_detalle"
        accion="Recargar"
        :accion_deshabilitada="cambiando"
        @accion="detalle_id && abrir_detalle(detalle_id)"
      />

      <comp-esqueleto v-if="!detalle && cargando_detalle" variante="detalle" :filas="3" />

      <template v-if="detalle">
        <header class="detalle-cabecera">
          <h2>{{ detalle.cliente }}</h2>
          <p class="app-texto-suave">{{ detalle.nombre_empresa }} · {{ fecha(detalle.fecha) }}</p>
          <div class="app-acciones">
            <comp-estado v-if="detalle.sin_enviar" :sin_enviar="true" />
            <comp-estado v-else :estado="detalle.estado" />
          </div>
        </header>

        <ion-list class="detalle-items">
          <ion-item v-for="item in detalle.items" :key="item.id">
            <ion-label class="ion-text-wrap">
              <h3>{{ item.nombre }}</h3>
              <p>{{ item.cantidad }} × {{ dinero(item.precioUnitario) }}</p>
            </ion-label>
            <ion-note slot="end" class="subtotal">{{ dinero(item.subtotal) }}</ion-note>
          </ion-item>
        </ion-list>

        <div class="total">
          <span>Total</span>
          <strong>{{ dinero(detalle.total) }}</strong>
        </div>

        <comp-aviso v-if="error_archivo" tipo="peligro" :mensaje="error_archivo" />

        <div v-if="!offline.sin_conexion.value" class="app-acciones archivos">
          <ion-button fill="outline" :disabled="cargando_qr" @click="mostrar_qr">
            <ion-icon slot="start" :icon="qrCodeOutline" />
            {{ cargando_qr ? 'Cargando QR...' : qr_url ? 'Ocultar QR' : 'Ver QR' }}
          </ion-button>
          <ion-button v-if="puede_pdf && detalle.estado === 'Listo'" fill="outline" :disabled="compartiendo" @click="compartir">
            <ion-icon slot="start" :icon="documentTextOutline" />
            {{ compartiendo ? 'Obteniendo PDF...' : es_nativo ? 'Compartir PDF' : 'Descargar PDF' }}
          </ion-button>
        </div>

        <img v-if="qr_url && !offline.sin_conexion.value" class="qr" :src="qr_url" :alt="`QR de ${codigo_pedido(detalle.id)}`" />
      </template>

      <template v-if="puede_estado && detalle?.estado === 'En preparacion'" #pie>
        <div class="pie-acciones">
          <ion-button expand="block" :disabled="cambiando" @click="cambiar_estado('Listo')">
            {{ cambiando ? 'Guardando...' : 'Marcar listo' }}
          </ion-button>
          <ion-button expand="block" color="danger" fill="outline" :disabled="cambiando" @click="cambiar_estado('Cancelado')">
            Cancelar pedido
          </ion-button>
        </div>
      </template>
    </comp-detalle-hoja>
  </comp-page>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { Capacitor } from '@capacitor/core';
import {
  IonButton,
  IonIcon,
  IonInput,
  IonItem,
  IonLabel,
  IonList,
  IonNote,
  onIonViewWillEnter,
  onIonViewWillLeave,
} from '@ionic/vue';
import { documentTextOutline, qrCodeOutline } from 'ionicons/icons';
import CompPage from '../components/base/comp-page.vue';
import CompLista from '../components/base/comp-lista.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import CompEstado from '../components/base/comp-estado.vue';
import CompAviso from '../components/base/comp-aviso.vue';
import CompDetalleHoja from '../components/base/comp-detalle-hoja.vue';
import CompPedidoFormulario from '../components/comp-pedido-formulario.vue';
import { sesion } from '../services/auth_service';
import { obtener_pedidos, obtener_pedido, cambiar_estado_pedido, obtener_qr_pedido, obtener_comprobante_pedido, codigo_pedido, parsear_codigo_pedido, type Pedido, type PedidoDetalle, type EstadoPedido } from '../services/pedidos_service';
import { compartir_pdf } from '../services/compartir_service';
import { escanear_pedido } from '../services/escaner_service';
import { avisar } from '../services/aviso_service';
import { con_cambios_sin_enviar, use_offline_store } from '../stores/offline_store';

const es_nativo = Capacitor.isNativePlatform();
const offline = use_offline_store();
const route = useRoute(), activo = ref(false);
const puede_crear = computed(() => ['superadmin', 'administrador', 'vendedor'].includes(sesion.usuario.value?.rol || ''));
const puede_estado = computed(() => ['superadmin', 'administrador', 'vendedor', 'caja'].includes(sesion.usuario.value?.rol || ''));
const puede_pdf = computed(() => ['superadmin', 'administrador', 'vendedor'].includes(sesion.usuario.value?.rol || ''));
const pedidos = ref<Pedido[]>([]), detalle = ref<PedidoDetalle | null>(null), detalle_id = ref<number | null>(null);
const codigo = ref(''), qr_url = ref('');
const cargando = ref(false), cargando_detalle = ref(false);
const cambiando = ref(false), cargando_qr = ref(false), compartiendo = ref(false), escaneando = ref(false);
const error_lista = ref(''), error_detalle = ref(''), error_codigo = ref(''), error_archivo = ref('');
let lista_version = 0, detalle_version = 0, qr_version = 0;
let vida = new AbortController(), archivo = new AbortController();
const dinero = (valor: number) => Number.isFinite(valor) ? new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(valor) : 'Cantidad invalida';
const fecha = (valor: string) => new Date(valor).toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'short' });

const resumen = computed(() => {
  if (cargando.value && pedidos.value.length === 0) return 'Cargando pedidos...';
  if (pedidos.value.length === 0) return 'Sin pedidos por ahora';
  const en_preparacion = pedidos.value.filter(p => p.estado === 'En preparacion').length;
  const listos = pedidos.value.filter(p => p.estado === 'Listo').length;
  return `${pedidos.value.length} pedido${pedidos.value.length === 1 ? '' : 's'} · ${en_preparacion} en preparación · ${listos} listo${listos === 1 ? '' : 's'}`;
});

function mensaje(error: unknown) {
  if (error && typeof error === 'object' && 'mensaje' in error) return String(error.mensaje);
  return error instanceof Error ? error.message : 'No se pudo completar la operacion.';
}

async function cargar() {
  const version = ++lista_version;
  cargando.value = true; error_lista.value = '';
  try {
    const respuesta = await obtener_pedidos();
    if (version === lista_version) pedidos.value = respuesta.pedidos.map(con_cambios_sin_enviar);
  }
  catch (error) { if (version === lista_version) error_lista.value = mensaje(error); }
  finally { if (version === lista_version) cargando.value = false; }
}
async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}
async function pedido_creado(id: number) {
  const signal = vida.signal;
  await abrir_detalle(id);
  if (!signal.aborted) await cargar();
}
function abrir_pedido_consulta() {
  const parametro = route.query.pedido;
  if (parametro === undefined) return;
  error_codigo.value = '';
  const id = typeof parametro === 'string' && /^[1-9]\d*$/.test(parametro) ? Number(parametro) : NaN;
  if (!Number.isSafeInteger(id)) { error_codigo.value = 'ID de pedido invalido.'; return; }
  codigo.value = codigo_pedido(id);
  void abrir_detalle(id);
}
function ocultar_qr() {
  ++qr_version; cargando_qr.value = false;
  if (qr_url.value) URL.revokeObjectURL(qr_url.value);
  qr_url.value = '';
}
function cerrar_detalle() {
  ++detalle_version; ocultar_qr(); archivo.abort(); archivo = new AbortController();
  detalle.value = null; detalle_id.value = null; cargando_detalle.value = false;
  error_detalle.value = ''; error_archivo.value = ''; compartiendo.value = false; cambiando.value = false;
}
async function abrir_detalle(id: number) {
  cerrar_detalle();
  const version = detalle_version;
  detalle_id.value = id; cargando_detalle.value = true;
  try {
    const respuesta = await obtener_pedido(id);
    if (version === detalle_version) detalle.value = con_cambios_sin_enviar(respuesta);
  }
  catch (error) { if (version === detalle_version) error_detalle.value = mensaje(error); }
  finally { if (version === detalle_version) cargando_detalle.value = false; }
}
function abrir_codigo() {
  error_codigo.value = '';
  try { void abrir_detalle(parsear_codigo_pedido(codigo.value.trim())); }
  catch (error) { error_codigo.value = mensaje(error); }
}
async function escanear() {
  if (escaneando.value) return;
  const signal = vida.signal, version = detalle_version;
  escaneando.value = true; error_codigo.value = '';
  try { const id = await escanear_pedido(signal); if (id !== null && !signal.aborted && version === detalle_version) { codigo.value = codigo_pedido(id); await abrir_detalle(id); } }
  catch (error) { if (!signal.aborted && version === detalle_version) error_codigo.value = mensaje(error); }
  finally { escaneando.value = false; }
}

function confirmar_cambio(estado: EstadoPedido) {
  const pregunta = estado === 'Cancelado'
    ? '¿Cancelar este pedido? Esta acción no se puede deshacer.'
    : '¿Marcar el pedido como Listo? Después ya no se puede modificar.';
  return window.confirm(pregunta);
}

async function cambiar_estado(estado: EstadoPedido) {
  const actual = detalle.value;
  if (!actual || cambiando.value || !puede_estado.value || actual.estado !== 'En preparacion') return;
  if (!confirmar_cambio(estado)) return;

  // Sin red el cambio no sale: espera en la cola y se dibuja con el estado nuevo.
  if (offline.sin_conexion.value) {
    offline.encolar(actual.id, estado);
    detalle.value = con_cambios_sin_enviar(actual);
    pedidos.value = pedidos.value.map(con_cambios_sin_enviar);
    void avisar('Cambio guardado sin conexión. Se envía solo cuando vuelva la red.');
    return;
  }

  const version = detalle_version;
  cambiando.value = true; error_detalle.value = '';
  try {
    const respuesta = await cambiar_estado_pedido(actual.id, estado);
    if (version !== detalle_version) return;
    detalle.value = respuesta;
    await cargar();
  } catch (error) {
    if (version !== detalle_version) return;
    error_detalle.value = mensaje(error);
    // Se recarga para mostrar el estado real sin borrar el mensaje de error.
    try { const respuesta = await obtener_pedido(actual.id); if (version === detalle_version) detalle.value = respuesta; } catch { /* Queda disponible reintentar. */ }
  } finally { if (version === detalle_version) cambiando.value = false; }
}
async function mostrar_qr() {
  if (!detalle.value || cargando_qr.value) return;
  if (qr_url.value) { ocultar_qr(); return; }
  const version = qr_version, id = detalle.value.id;
  cargando_qr.value = true; error_archivo.value = '';
  try {
    const blob = await obtener_qr_pedido(id);
    if (version === qr_version) qr_url.value = URL.createObjectURL(blob);
  } catch (error) { if (version === qr_version) error_archivo.value = mensaje(error); }
  finally { if (version === qr_version) cargando_qr.value = false; }
}
async function compartir() {
  if (!puede_pdf.value || !detalle.value || detalle.value.estado !== 'Listo' || compartiendo.value) return;
  const id = detalle.value.id, signal = archivo.signal;
  compartiendo.value = true; error_archivo.value = '';
  try {
    const blob = await obtener_comprobante_pedido(id);
    if (!signal.aborted) await compartir_pdf(blob, `${codigo_pedido(id)}.pdf`, signal);
  } catch (error) { if (!signal.aborted) error_archivo.value = mensaje(error); }
  finally { if (!signal.aborted) compartiendo.value = false; }
}
function salir() {
  activo.value = false; vida.abort(); ++lista_version; cerrar_detalle();
  cargando.value = false; pedidos.value = [];
}
watch(() => route.query.pedido, () => { if (activo.value) abrir_pedido_consulta(); });
// Cuando la cola baja, la lista se recarga sola y muestra lo que de verdad quedo.
watch(() => offline.ultima_sincronizacion.value, () => {
  if (!activo.value) return;
  void cargar();
  if (detalle_id.value !== null) void abrir_detalle(detalle_id.value);
});
onIonViewWillEnter(() => {
  activo.value = true; vida = new AbortController(); void cargar(); abrir_pedido_consulta();
  // Por si quedo algo en la cola y la red volvio con la app cerrada.
  void offline.sincronizar();
});
onIonViewWillLeave(salir);
onBeforeUnmount(salir);
</script>

<style scoped>
.encabezado {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: var(--app-espacio-3);
  flex-wrap: wrap;
}

.encabezado h1 {
  margin: 0;
  font-size: var(--app-texto-xl);
  font-weight: 800;
  color: var(--app-texto);
}

.encabezado p { margin: 2px 0 0; }

.buscador {
  display: flex;
  flex-direction: column;
  gap: var(--app-espacio-3);
  padding: var(--app-espacio-4);
}

.lista-pedidos {
  margin: 0;
  border-radius: var(--app-radio-md);
  overflow: hidden;
  border: 1px solid var(--app-borde);
  box-shadow: var(--app-sombra-1);
}

.pedido { --padding-start: var(--app-espacio-3); --inner-padding-end: var(--app-espacio-3); }

.pedido-titulo {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--app-espacio-2);
  flex-wrap: wrap;
}

.pedido-titulo h2 { margin: 0; font-size: var(--app-texto-md); font-weight: 700; }
.pedido-fecha { color: var(--app-texto-suave); font-size: var(--app-texto-xs); }
.pedido-total { font-weight: 700; color: var(--app-texto); }

.detalle-cabecera { display: flex; flex-direction: column; gap: var(--app-espacio-1); margin-bottom: var(--app-espacio-4); }
.detalle-cabecera h2 { margin: 0; font-size: var(--app-texto-lg); }
.detalle-cabecera p { margin: 0; }

.detalle-items {
  margin: 0;
  border: 1px solid var(--app-borde);
  border-radius: var(--app-radio-sm);
  overflow: hidden;
}

.subtotal { font-weight: 600; }

.total {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--app-espacio-3) var(--app-espacio-1);
  font-size: var(--app-texto-lg);
}

.total strong { font-size: var(--app-texto-xl); }

.archivos { margin-top: var(--app-espacio-3); }

.qr { display: block; width: min(100%, 260px); height: auto; margin: var(--app-espacio-4) auto; }

.pie-acciones { display: flex; flex-direction: column; gap: var(--app-espacio-2); padding: var(--app-espacio-2); }
.pie-acciones ion-button { margin: 0; }
</style>
