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
import { use_notificaciones_store } from '../../stores/notificaciones_store';
import { use_offline_store } from '../../stores/offline_store';

const { no_leidas } = use_notificaciones_store();
const offline_store = use_offline_store();
const etiqueta_campana = computed(() => no_leidas.value > 0
  ? `Notificaciones, ${no_leidas.value} sin leer`
  : 'Notificaciones');

defineProps({
  titulo: {
    type: String,
    required: true,
  },
  mostrar_actualizar: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(['actualizar']);

function actualizar(event: CustomEvent) {
  emit('actualizar', event);
}
</script>

<style scoped>
.contenido {
  padding: 16px;
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
  border-radius: 8px;
  font-size: 10px;
  line-height: 16px;
}
</style>
