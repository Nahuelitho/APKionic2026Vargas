<template>
  <comp-page titulo="Inicio">
    <header class="saludo">
      <div>
        <p class="app-texto-suave">Hola{{ nombre ? `, ${nombre}` : '' }}</p>
        <h1>Resumen del día</h1>
        <p class="app-texto-suave">{{ etiqueta_ambito }}</p>
      </div>
      <ion-button fill="outline" router-link="/app/pedidos">Ver pedidos</ion-button>
    </header>

    <div class="indicadores">
      <button class="indicador app-tarjeta" type="button" @click="ir_a_pedidos()">
        <ion-icon :icon="receiptOutline" class="indicador-icono" aria-hidden="true" />
        <strong>{{ cargando && !pedidos.length ? '—' : pedidos.length }}</strong>
        <span>Pedidos</span>
      </button>
      <button class="indicador app-tarjeta" type="button" @click="ir_a_pedidos">
        <ion-icon :icon="timeOutline" class="indicador-icono tono-aviso" aria-hidden="true" />
        <strong>{{ en_preparacion }}</strong>
        <span>En preparación</span>
      </button>
      <button class="indicador app-tarjeta" type="button" @click="ir_a_pedidos">
        <ion-icon :icon="checkmarkCircleOutline" class="indicador-icono tono-exito" aria-hidden="true" />
        <strong>{{ listos }}</strong>
        <span>Listos</span>
      </button>
    </div>

    <comp-aviso v-if="error" tipo="peligro" titulo="No se pudo cargar el resumen" :mensaje="error" accion="Reintentar" @accion="cargar" />
    <comp-esqueleto v-if="cargando && !pedidos.length" :filas="3" />

    <section v-else class="ultimos">
      <div class="ultimos-cabecera">
        <h2 class="app-titulo-seccion">Últimos pedidos</h2>
        <ion-button fill="clear" size="small" router-link="/app/pedidos">Ver todos</ion-button>
      </div>

      <comp-aviso
        v-if="!pedidos.length"
        tipo="info"
        :mensaje="puede_crear ? 'Todavía no hay pedidos. Creá el primero desde la pestaña Pedidos.' : 'Todavía no hay pedidos para mostrar.'"
      />

      <ion-list v-else class="lista-ultimos">
        <ion-item v-for="pedido in ultimos" :key="pedido.id" button :router-link="`/app/pedidos?pedido=${pedido.id}`">
          <ion-label class="ion-text-wrap">
            <h3>{{ codigo_pedido(pedido.id) }} · {{ pedido.cliente }}</h3>
            <p>{{ dinero(pedido.total) }} · {{ fecha(pedido.fecha) }}</p>
          </ion-label>
          <comp-estado slot="end" :estado="pedido.estado" />
        </ion-item>
      </ion-list>
    </section>

    <section class="conexion app-tarjeta">
      <div class="conexion-cabecera">
        <ion-icon :icon="globeOutline" aria-hidden="true" />
        <div>
          <h2 class="app-titulo-seccion">Conexión con la API</h2>
          <p class="app-texto-suave">{{ api_url }}</p>
        </div>
      </div>
      <div class="app-acciones">
        <ion-button fill="outline" size="small" :disabled="probando_api" @click="probar_conexion_api">
          {{ probando_api ? 'Probando...' : 'Probar conexión' }}
        </ion-button>
      </div>
      <comp-aviso
        v-if="mensaje_api"
        :tipo="api_ok ? 'exito' : 'peligro'"
        :mensaje="mensaje_api"
      />
    </section>
  </comp-page>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  IonButton,
  IonIcon,
  IonItem,
  IonLabel,
  IonList,
  onIonViewWillEnter,
} from '@ionic/vue';
import {
  checkmarkCircleOutline,
  globeOutline,
  receiptOutline,
  timeOutline,
} from 'ionicons/icons';
import CompPage from '../components/base/comp-page.vue';
import CompAviso from '../components/base/comp-aviso.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import CompEstado from '../components/base/comp-estado.vue';
import { sesion } from '../services/auth_service';
import { obtener_api_url } from '../config/debug';
import { probar_api } from '../services/api_service';
import { codigo_pedido, obtener_pedidos, type Pedido } from '../services/pedidos_service';

const router = useRouter();
const api_url = obtener_api_url();
const pedidos = ref<Pedido[]>([]);
const cargando = ref(false);
const error = ref('');
const probando_api = ref(false);
const mensaje_api = ref<string | null>(null);
const api_ok = ref<boolean | null>(null);

const nombre = computed(() => sesion.usuario.value?.nombre?.split(' ')[0] || '');
const puede_crear = computed(() => ['superadmin', 'administrador', 'vendedor'].includes(sesion.usuario.value?.rol || ''));
const etiqueta_ambito = computed(() => {
  const usuario = sesion.usuario.value;
  if (!usuario?.rol) return 'Catálogo de tiendas';
  return usuario.empresa_id ? `${usuario.rol} · Empresa ${usuario.empresa_id}` : usuario.rol;
});
const en_preparacion = computed(() => pedidos.value.filter(p => p.estado === 'En preparacion').length);
const listos = computed(() => pedidos.value.filter(p => p.estado === 'Listo').length);
const ultimos = computed(() => pedidos.value.slice(0, 5));
const dinero = (valor: number) => Number.isFinite(valor)
  ? new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(valor)
  : '—';
const fecha = (valor: string) => new Date(valor).toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'short' });

function ir_a_pedidos() {
  void router.push('/app/pedidos');
}

function mensaje(error: unknown) {
  if (error && typeof error === 'object' && 'mensaje' in error) return String(error.mensaje);
  return error instanceof Error ? error.message : 'No se pudo cargar el resumen.';
}

async function cargar() {
  cargando.value = true;
  error.value = '';
  try {
    pedidos.value = (await obtener_pedidos()).pedidos;
  } catch (causa) {
    pedidos.value = [];
    error.value = mensaje(causa);
  } finally {
    cargando.value = false;
  }
}

async function probar_conexion_api() {
  probando_api.value = true;
  mensaje_api.value = null;
  api_ok.value = null;
  try {
    const respuesta = await probar_api();
    api_ok.value = true;
    mensaje_api.value = `API conectada. Estado ${respuesta.status}.`;
  } catch (causa) {
    api_ok.value = false;
    mensaje_api.value = mensaje(causa);
  } finally {
    probando_api.value = false;
  }
}

onIonViewWillEnter(cargar);
</script>

<style scoped>
.saludo {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: var(--app-espacio-3);
  flex-wrap: wrap;
}

.saludo h1 {
  margin: 0;
  font-size: var(--app-texto-xl);
  font-weight: 800;
  color: var(--app-texto);
}

.saludo p { margin: 2px 0 0; }

.indicadores {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(96px, 1fr));
  gap: var(--app-espacio-3);
}

.indicador {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--app-espacio-1);
  padding: var(--app-espacio-3);
  border: 1px solid var(--app-borde);
  text-align: left;
  cursor: pointer;
  font: inherit;
  color: var(--app-texto);
}

.indicador:active { transform: scale(0.98); }

.indicador strong { font-size: var(--app-texto-xl); line-height: 1.1; }
.indicador span { color: var(--app-texto-suave); font-size: var(--app-texto-xs); }
.indicador-icono { font-size: 22px; color: var(--ion-color-primary); }
.indicador-icono.tono-aviso { color: var(--app-estado-aviso-texto); }
.indicador-icono.tono-exito { color: var(--app-estado-exito-texto); }

.ultimos { display: flex; flex-direction: column; gap: var(--app-espacio-2); }

.ultimos-cabecera {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--app-espacio-2);
}

.lista-ultimos {
  margin: 0;
  border-radius: var(--app-radio-md);
  overflow: hidden;
  border: 1px solid var(--app-borde);
  box-shadow: var(--app-sombra-1);
}

.lista-ultimos h3 { font-size: var(--app-texto-sm); font-weight: 700; }
.lista-ultimos p { font-size: var(--app-texto-xs); color: var(--app-texto-suave); }

.conexion { display: flex; flex-direction: column; gap: var(--app-espacio-2); padding: var(--app-espacio-4); }

.conexion-cabecera { display: flex; align-items: flex-start; gap: var(--app-espacio-2); }
.conexion-cabecera ion-icon { font-size: 22px; color: var(--ion-color-primary); margin-top: 2px; }
.conexion-cabecera p { margin: 2px 0 0; word-break: break-all; }
</style>
