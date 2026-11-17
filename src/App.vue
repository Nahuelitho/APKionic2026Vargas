<template>
  <ion-app>
    <ion-router-outlet />
  </ion-app>
</template>

<script setup lang="ts">
import { IonApp, IonRouterOutlet } from '@ionic/vue';
import { onBeforeUnmount, watch } from 'vue';
import { useRouter } from 'vue-router';
import { sesion } from '@/services/auth_service';
import { use_notificaciones_store } from '@/stores/notificaciones_store';
import {
  conectar_navegacion,
  intentar_navegacion_pendiente,
  preparar_push,
  registrar_sesion_push,
} from '@/services/push_service';

const router = useRouter();
const { iniciar_polling, detener_polling } = use_notificaciones_store();

// La push necesita el router para abrir el pedido; se conecta antes de montar.
conectar_navegacion(router);

// Se registra antes de que el router este listo para no perder el toque
// de una push que abrio la app cerrada.
void preparar_push();

// La campana se refresca sola mientras haya sesion iniciada.
watch(() => sesion.autenticado.value, (autenticado) => {
  if (autenticado) {
    iniciar_polling();
    void registrar_sesion_push();
    intentar_navegacion_pendiente();
  } else {
    detener_polling();
  }
}, { immediate: true });

onBeforeUnmount(detener_polling);
</script>
