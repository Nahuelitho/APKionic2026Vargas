<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>

        <ion-title>{{ titulo }}</ion-title>

        <ion-buttons slot="end">
          <ion-button v-if="!offline_store.sin_conexion.value" class="campana" router-link="/app/notificaciones" :aria-label="etiqueta_campana">
            <ion-icon slot="icon-only" :icon="notificationsOutline" />
            <ion-badge v-if="no_leidas > 0" color="danger" class="campana-badge">{{ no_leidas > 99 ? '99+' : no_leidas }}</ion-badge>
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content>
      <ion-refresher
        v-if="mostrar_actualizar"
        slot="fixed"
        @ionRefresh="actualizar"
      >
        <ion-refresher-content />
      </ion-refresher>

      <div class="contenido">
        <!-- Un solo aviso de red para todas las pantallas. -->
        <comp-aviso
          v-if="offline_store.sin_conexion.value"
          tipo="sin-red"
          titulo="Sin conexión"
          :mensaje="aviso_offline"
        />
        <slot />
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import {
  IonBadge,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonMenuButton,
  IonPage,
  IonRefresher,
  IonRefresherContent,
  IonTitle,
  IonToolbar,
} from '@ionic/vue';
import { notificationsOutline } from 'ionicons/icons';
import CompAviso from './comp-aviso.vue';
import { use_notificaciones_store } from '../../stores/notificaciones_store';
import { use_offline_store } from '../../stores/offline_store';

defineProps({
  titulo: {
    type: String,
    required: true,
  },
  mostrar_actualizar: {
    type: Boolean,
    default: false,
  },
  aviso_offline: {
    type: String,
    default: 'Estás viendo los datos guardados. Los cambios se envían solos cuando vuelva la red.',
  },
});

const { no_leidas } = use_notificaciones_store();
const offline_store = use_offline_store();
const etiqueta_campana = computed(() => no_leidas.value > 0
  ? `Notificaciones, ${no_leidas.value} sin leer`
  : 'Notificaciones');

const emit = defineEmits(['actualizar']);

function actualizar(event: CustomEvent) {
  emit('actualizar', event);
}
</script>

<style scoped>
.contenido {
  display: flex;
  flex-direction: column;
  gap: var(--app-espacio-3);
  padding: var(--app-espacio-4);
  padding-bottom: calc(var(--app-espacio-5) + env(safe-area-inset-bottom));
}

.campana {
  position: relative;
}

.campana-badge {
  position: absolute;
  top: 4px;
  right: 4px;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: var(--app-radio-pill);
  font-size: 10px;
  line-height: 16px;
}
</style>
