
<template>
  <ion-app>
    <ion-router-outlet />
  </ion-app>
</template>

<script setup lang="ts">
import { IonApp, IonRouterOutlet } from '@ionic/vue';
import { onBeforeUnmount, onMounted, watch } from 'vue';
import { sesion } from '@/services/auth_service';
import { preparar_push } from '@/services/notificaciones_nativas_service';
import { use_notificaciones_store } from '@/stores/notificaciones_store';

const { iniciar_polling, detener_polling } = use_notificaciones_store();

// La campana se refresca sola mientras haya sesion iniciada.
watch(() => sesion.autenticado.value, (autenticado) => {
  if (autenticado) iniciar_polling();
  else detener_polling();
}, { immediate: true });

onMounted(() => {
  preparar_push();
});

onBeforeUnmount(detener_polling);
</script>
