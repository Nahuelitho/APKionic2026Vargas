<template>
  <comp-page :titulo="sesion.usuario.value?.rol ? 'Pedidos' : 'Mis pedidos'" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-card>
      <ion-card-content>
        <div class="acciones">
          <ion-button v-if="es_nativo" :disabled="escaneando" @click="escanear">{{ escaneando ? 'Preparando escaner...' : 'Escanear QR' }}</ion-button>
          <ion-input v-model="codigo" label="Codigo de pedido" label-placement="stacked" placeholder="PED-0007" @keyup.enter="abrir_codigo" />
          <ion-button fill="outline" :disabled="escaneando || !codigo" @click="abrir_codigo">Abrir codigo</ion-button>
        </div>
        <ion-text v-if="error_codigo" color="danger"><p role="alert">{{ error_codigo }}</p></ion-text>
      </ion-card-content>
    </ion-card>

    <ion-button v-if="sesion.usuario.value && !sesion.usuario.value.rol" router-link="/app/comprar">Elegir empresa</ion-button>
    <comp-pedido-formulario v-if="activo && puede_crear" @creado="pedido_creado" />

    <ion-card v-if="detalle_id">
      <ion-card-header><ion-card-title>{{ codigo_pedido(detalle_id) }}</ion-card-title></ion-card-header>
      <ion-card-content>
        <ion-button fill="clear" @click="cerrar_detalle">Cerrar detalle</ion-button>
        <p v-if="cargando_detalle">Cargando detalle...</p>
        <ion-text v-if="error_detalle" color="danger"><p role="alert">{{ error_detalle }}</p></ion-text>
        <ion-button v-if="error_detalle" fill="outline" :disabled="cambiando" @click="abrir_detalle(detalle_id)">Recargar detalle</ion-button>
        <template v-if="detalle">
          <h2>{{ detalle.cliente }}</h2>
          <p>{{ detalle.nombre_empresa }}</p>
          <p>{{ fecha(detalle.fecha) }}</p>
          <ion-badge :color="color_estado(detalle.estado)">{{ detalle.estado }}</ion-badge>
          <ion-list>
            <ion-item v-for="item in detalle.items" :key="item.id">
              <ion-label class="ion-text-wrap"><h3>{{ item.nombre }}</h3><p>{{ item.cantidad }} x {{ dinero(item.precioUnitario) }}</p><p>{{ dinero(item.subtotal) }}</p></ion-label>
            </ion-item>
          </ion-list>
          <p><strong>Total: {{ dinero(detalle.total) }}</strong></p>
          <div v-if="puede_estado && detalle.estado === 'En preparacion'" class="acciones">
            <ion-button :disabled="cambiando" @click="cambiar_estado('Listo')">Marcar Listo</ion-button>
            <ion-button color="danger" fill="outline" :disabled="cambiando" @click="cambiar_estado('Cancelado')">Cancelar pedido</ion-button>
          </div>
          <div class="acciones">
            <ion-button fill="outline" :disabled="cargando_qr" @click="mostrar_qr">{{ cargando_qr ? 'Cargando QR...' : 'Ver QR' }}</ion-button>
            <ion-button v-if="puede_pdf && detalle.estado === 'Listo'" :disabled="compartiendo" @click="compartir">{{ compartiendo ? 'Obteniendo PDF...' : es_nativo ? 'Compartir PDF' : 'Descargar PDF' }}</ion-button>
          </div>
          <img v-if="qr_url" class="qr" :src="qr_url" :alt="`QR de ${codigo_pedido(detalle.id)}`" />
          <ion-button v-if="qr_url || cargando_qr" fill="clear" @click="ocultar_qr">Ocultar QR</ion-button>
          <ion-text v-if="error_archivo" color="danger"><p role="alert">{{ error_archivo }}</p></ion-text>
        </template>
      </ion-card-content>
    </ion-card>

    <ion-text v-if="error_lista" color="danger"><p role="alert">{{ error_lista }}</p></ion-text>
    <ion-button v-if="error_lista" fill="outline" @click="cargar">Reintentar pedidos</ion-button>
    <comp-esqueleto v-if="cargando && !pedidos.length" />
    <p v-else-if="!pedidos.length && !error_lista">No hay pedidos cargados.</p>
    <ion-list v-else>
      <ion-item v-for="pedido in pedidos" :key="pedido.id" button :detail="true" @click="abrir_detalle(pedido.id)">
        <ion-label class="ion-text-wrap"><h3>{{ codigo_pedido(pedido.id) }} · {{ pedido.cliente }}</h3><p>{{ pedido.nombre_empresa }}</p><p>{{ fecha(pedido.fecha) }}</p><p>{{ dinero(pedido.total) }}</p></ion-label>
        <ion-badge :color="color_estado(pedido.estado)">{{ pedido.estado }}</ion-badge>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { Capacitor } from '@capacitor/core';
import { IonBadge, IonButton, IonCard, IonCardContent, IonCardHeader, IonCardTitle, IonInput, IonItem, IonLabel, IonList, IonText, onIonViewWillEnter, onIonViewWillLeave } from '@ionic/vue';
import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import CompPedidoFormulario from '../components/comp-pedido-formulario.vue';
import { sesion } from '../services/auth_service';
import { obtener_pedidos, obtener_pedido, cambiar_estado_pedido, obtener_qr_pedido, obtener_comprobante_pedido, codigo_pedido, parsear_codigo_pedido, type Pedido, type PedidoDetalle, type EstadoPedido } from '../services/pedidos_service';
import { compartir_pdf } from '../services/compartir_service';
import { escanear_pedido } from '../services/escaner_service';

const es_nativo = Capacitor.isNativePlatform();
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
const fecha = (valor: string) => new Date(valor).toLocaleString('es-AR');
const color_estado = (estado: EstadoPedido) => estado === 'Listo' ? 'success' : estado === 'Cancelado' ? 'danger' : 'warning';
function mensaje(error: unknown) {
  if (error && typeof error === 'object' && 'mensaje' in error) return String(error.mensaje);
  return error instanceof Error ? error.message : 'No se pudo completar la operacion.';
}

async function cargar() {
  const version = ++lista_version;
  cargando.value = true; error_lista.value = '';
  try { const respuesta = await obtener_pedidos(); if (version === lista_version) pedidos.value = respuesta.pedidos; }
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
  try { const respuesta = await obtener_pedido(id); if (version === detalle_version) detalle.value = respuesta; }
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
async function cambiar_estado(estado: EstadoPedido) {
  const actual = detalle.value;
  if (!actual || cambiando.value || !puede_estado.value || actual.estado !== 'En preparacion') return;
  if (estado === 'Cancelado' && !window.confirm('Cancelar este pedido? Esta accion no se puede deshacer.')) return;
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
    // Reload after a concurrency conflict without hiding the original error.
    try { const respuesta = await obtener_pedido(actual.id); if (version === detalle_version) detalle.value = respuesta; } catch { /* Keep retry available. */ }
  } finally { if (version === detalle_version) cambiando.value = false; }
}
async function mostrar_qr() {
  if (!detalle.value || cargando_qr.value) return;
  ocultar_qr();
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
onIonViewWillEnter(() => {
  activo.value = true; vida = new AbortController(); void cargar(); abrir_pedido_consulta();
});
onIonViewWillLeave(salir);
onBeforeUnmount(salir);
</script>

<style scoped>
.acciones { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
.acciones ion-input { flex: 1 1 180px; }
.qr { display: block; width: min(100%, 280px); height: auto; margin: 16px auto; }
@media (max-width: 600px) {
  ion-item ion-badge { max-width: 110px; white-space: normal; text-align: center; }
}
</style>
