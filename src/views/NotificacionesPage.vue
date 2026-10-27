<template>
  <comp-page titulo="Notificaciones" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-card v-if="no_leidas > 0">
      <ion-card-content class="cabecera">
        <span>{{ no_leidas }} sin leer</span>
        <ion-button size="small" fill="outline" :disabled="marcando" @click="marcar_todas">
          {{ marcando ? 'Marcando...' : 'Marcar todas como leidas' }}
        </ion-button>
      </ion-card-content>
    </ion-card>

    <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
    <ion-button v-if="error" fill="outline" :disabled="cargando" @click="cargar_notificaciones">Reintentar</ion-button>

    <comp-esqueleto v-if="cargando && !notificaciones.length" />
    <p v-else-if="!notificaciones.length && !error">No tenes notificaciones.</p>

    <ion-list v-else>
      <ion-item
        v-for="notificacion in notificaciones"
        :key="notificacion.id"
        button
        @click="abrir(notificacion)"
      >
        <ion-icon
          slot="start"
          :icon="notificacion.leida ? mailOpenOutline : mailUnreadOutline"
          :color="notificacion.leida ? 'medium' : 'primary'"
        />
        <ion-label class="ion-text-wrap" :class="{ 'sin-leer': !notificacion.leida }">
          <h3>{{ notificacion.titulo }}</h3>
          <p>{{ notificacion.mensaje }}</p>
          <p class="fecha">{{ fecha(notificacion.fecha) }}</p>
        </ion-label>
        <ion-badge v-if="!notificacion.leida" color="danger">Nueva</ion-badge>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import {
  IonBadge,
  IonButton,
  IonCard,
  IonCardContent,
  IonIcon,
  IonItem,
  IonLabel,
  IonList,
  IonText,
  onIonViewWillEnter,
} from '@ionic/vue';
import { mailOpenOutline, mailUnreadOutline } from 'ionicons/icons';
import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
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
  await marcar_leida(notificacion.id);
  if (notificacion.pedido_id) {
    await router.push({ path: '/app/pedidos', query: { pedido: String(notificacion.pedido_id) } });
  }
}

onIonViewWillEnter(() => { void cargar_notificaciones(); });
</script>

<style scoped>
.cabecera { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
.sin-leer h3 { font-weight: 700; }
.fecha { color: var(--ion-color-medium); font-size: 0.8rem; }
ion-item ion-badge { max-width: 90px; white-space: normal; text-align: center; }
</style>
