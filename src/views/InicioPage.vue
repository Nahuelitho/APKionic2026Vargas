<template>
  <comp-page titulo="Inicio">
    <ion-card>
      <ion-card-header>
        <ion-card-title>App Multi-tenant</ion-card-title>
        <ion-card-subtitle>Vargas Nahuel</ion-card-subtitle>
      </ion-card-header>

      <ion-card-content>
        Bienvenido a la version 2. Las rutas, el menu lateral y las pestanas
        salen desde el archivo de configuracion.
      </ion-card-content>
    </ion-card>

    <ion-card>
      <ion-card-header>
        <ion-card-title>Conexion con API</ion-card-title>
      </ion-card-header>

      <ion-card-content>
        <p>URL configurada: {{ api_url }}</p>

        <ion-button
          expand="block"
          :disabled="probando_api"
          @click="probar_conexion_api"
        >
          {{ probando_api ? 'Probando...' : 'Probar conexion con API' }}
        </ion-button>

        <ion-text
          v-if="mensaje_api"
          :color="api_ok ? 'success' : 'danger'"
        >
          <p>{{ mensaje_api }}</p>
        </ion-text>
      </ion-card-content>
    </ion-card>
  </comp-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';

import {
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardSubtitle,
  IonCardTitle,
  IonText,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';
import { obtener_api_url } from '../config/debug';
import { probar_api } from '../services/api_service';

const probando_api = ref(false);
const mensaje_api = ref<string | null>(null);
const api_ok = ref<boolean | null>(null);
const api_url = obtener_api_url();

async function probar_conexion_api() {
  probando_api.value = true;
  mensaje_api.value = null;
  api_ok.value = null;

  try {
    const respuesta = await probar_api();

    api_ok.value = true;
    mensaje_api.value = `API conectada correctamente en ${api_url}. Estado: ${respuesta.status}`;
  } catch (error) {
    const ajax_error = error as { mensaje?: string };

    api_ok.value = false;
    mensaje_api.value = ajax_error.mensaje || `No se pudo conectar con la API en ${api_url}.`;
  } finally {
    probando_api.value = false;
  }
}
</script>
