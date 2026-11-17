<template>
  <ion-modal
    :is-open="abierto"
    :initial-breakpoint="0.92"
    :breakpoints="[0, 0.6, 0.92]"
    :backdrop-breakpoint="0.6"
    :handle="true"
    class="hoja"
    @didDismiss="emit('cerrar')"
  >
    <ion-header class="hoja-cabecera">
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-button aria-label="Cerrar detalle" @click="emit('cerrar')">
            <ion-icon slot="icon-only" :icon="closeOutline" />
          </ion-button>
        </ion-buttons>
        <ion-title>{{ titulo }}</ion-title>
        <ion-buttons slot="end">
          <slot name="acciones" />
        </ion-buttons>
      </ion-toolbar>
      <ion-progress-bar v-if="cargando" type="indeterminate" />
    </ion-header>

    <ion-content class="ion-padding">
      <slot />
    </ion-content>

    <ion-footer v-if="$slots.pie" class="hoja-pie">
      <ion-toolbar>
        <slot name="pie" />
      </ion-toolbar>
    </ion-footer>
  </ion-modal>
</template>

<script setup lang="ts">
import {
  IonButton,
  IonButtons,
  IonContent,
  IonFooter,
  IonHeader,
  IonIcon,
  IonModal,
  IonProgressBar,
  IonTitle,
  IonToolbar,
} from '@ionic/vue';
import { closeOutline } from 'ionicons/icons';

/**
 * Hoja de detalle reutilizable: se abre desde abajo, se cierra con
 * el boton, el gesto o tocando el fondo.
 */
defineProps({
  abierto: { type: Boolean, default: false },
  titulo: { type: String, default: 'Detalle' },
  cargando: { type: Boolean, default: false },
});

const emit = defineEmits(['cerrar']);
</script>

<style scoped>
.hoja {
  --border-radius: var(--app-radio-lg) var(--app-radio-lg) 0 0;
  --box-shadow: var(--app-sombra-2);
}

.hoja-cabecera ion-toolbar {
  --background: var(--app-tarjeta);
  --border-color: var(--app-borde);
}

.hoja-pie ion-toolbar {
  --background: var(--app-tarjeta);
  --border-color: var(--app-borde);
}
</style>
