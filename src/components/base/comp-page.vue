<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>

        <ion-title>{{ titulo }}</ion-title>
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
import {
  IonButtons,
  IonContent,
  IonHeader,
  IonMenuButton,
  IonPage,
  IonRefresher,
  IonRefresherContent,
  IonTitle,
  IonToolbar,
} from '@ionic/vue';

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
</style>