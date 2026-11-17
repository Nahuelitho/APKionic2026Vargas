<template>
  <comp-page titulo="Notificaciones" :mostrar_actualizar="true" @actualizar="actualizar">
    <header class="resumen">
      <div class="resumen-texto">
        <h1>Tu bandeja</h1>
        <p class="app-texto-suave">
          {{ no_leidas > 0 ? `${no_leidas} sin leer` : 'Estás al día' }}
        </p>
      </div>
      <ion-button
        v-if="no_leidas > 0"
        size="small"
        fill="outline"
        :disabled="marcando"
        @click="marcar_todas"
      >
        {{ marcando ? 'Marcando...' : 'Marcar todas' }}
      </ion-button>
    </header>

    <comp-lista
      :cargando="cargando"
      :error="error || ''"
      :items="notificaciones.length"
      texto_vacio="Todavía no tenés notificaciones. Cuando cambie un pedido, aparece acá y también como push."
      vacio_tipo="info"
      :filas_esqueleto="4"
      @reintentar="cargar_notificaciones"
    >
      <ion-list class="lista-notificaciones">
        <ion-item
          v-for="notificacion in notificaciones"
          :key="notificacion.id"
          button
          :detail="Boolean(notificacion.pedido_id)"
          class="notificacion"
          :class="{ 'sin-leer': !notificacion.leida }"
          @click="abrir(notificacion)"
        >
          <div slot="start" class="punto" :class="{ activo: !notificacion.leida }" aria-hidden="true" />
          <ion-label class="ion-text-wrap">
            <h2>{{ notificacion.titulo }}</h2>
            <p>{{ notificacion.mensaje }}</p>
            <p class="fecha">
              <ion-icon :icon="timeOutline" aria-hidden="true" />
              {{ fecha(notificacion.fecha) }}
            </p>
          </ion-label>
          <comp-estado slot="end" :definicion="notificacion.leida ? ESTADOS_NOTIFICACION.leida : ESTADOS_NOTIFICACION.nueva" />
        </ion-item>
      </ion-list>
    </comp-lista>
  </comp-page>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import {
  IonButton,
  IonIcon,
  IonItem,
  IonLabel,
  IonList,
  onIonViewWillEnter,
} from '@ionic/vue';
import { timeOutline } from 'ionicons/icons';
import CompPage from '../components/base/comp-page.vue';
import CompLista from '../components/base/comp-lista.vue';
import CompEstado from '../components/base/comp-estado.vue';
import { ESTADOS_NOTIFICACION } from '../config/estados';
import { use_notificaciones_store } from '../stores/notificaciones_store';
import type { Notificacion } from '../services/notificaciones_service';

const router = useRouter();
const {
  notificaciones,
  no_leidas,
  cargando,
  marcando,
  error,
  cargar_notificaciones,
  marcar_leida,
  marcar_todas,
} = use_notificaciones_store();

const fecha = (valor: string) => new Date(valor).toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'short' });

async function actualizar(event: CustomEvent) {
  try { await cargar_notificaciones(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}

async function abrir(notificacion: Notificacion) {
  // Primero se abre el dato aunque falle el "marcar leida": el toque nunca se pierde.
  if (!notificacion.leida) void marcar_leida(notificacion.id);
  if (notificacion.pedido_id) {
    await router.push({ path: '/app/pedidos', query: { pedido: String(notificacion.pedido_id) } });
  }
}

onIonViewWillEnter(() => { void cargar_notificaciones(); });
</script>

<style scoped>
.resumen {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--app-espacio-3);
  flex-wrap: wrap;
}

.resumen-texto h1 {
  margin: 0;
  font-size: var(--app-texto-xl);
  font-weight: 800;
  color: var(--app-texto);
}

.resumen-texto p { margin: 2px 0 0; }

.lista-notificaciones {
  margin: 0;
  border-radius: var(--app-radio-md);
  overflow: hidden;
  border: 1px solid var(--app-borde);
  box-shadow: var(--app-sombra-1);
}

.notificacion {
  --padding-start: var(--app-espacio-3);
  --inner-padding-end: var(--app-espacio-3);
}

.sin-leer { --background: var(--app-estado-info-fondo); }

.punto {
  width: 8px;
  height: 8px;
  border-radius: var(--app-radio-pill);
  background: var(--app-gris-300);
  align-self: flex-start;
  margin-top: 8px;
}

.punto.activo { background: var(--ion-color-primary); }

.notificacion h2 {
  font-size: var(--app-texto-md);
  font-weight: 500;
}

.sin-leer h2 { font-weight: 700; }

.fecha {
  display: flex;
  align-items: center;
  gap: 4px;
  color: var(--app-texto-suave);
  font-size: var(--app-texto-xs);
}

.fecha ion-icon { font-size: 12px; }
</style>
