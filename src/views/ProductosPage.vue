<template>
  <comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-card>
      <ion-card-header>
        <ion-card-title>Elegí una empresa</ion-card-title>
        <ion-card-subtitle>Los productos se muestran por empresa.</ion-card-subtitle>
      </ion-card-header>
      <ion-card-content>
        <p>
          Seleccioná una empresa para ver y gestionar sus productos.
          <template v-if="!es_superadmin">
            Solo vas a ver la empresa a la que pertenecés.
          </template>
        </p>
      </ion-card-content>
    </ion-card>

    <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
    <ion-button v-if="error" fill="outline" @click="cargar">Reintentar</ion-button>

    <comp-esqueleto v-if="cargando && !empresas.length" />
    <p v-if="!cargando && !error && !empresas.length">No hay empresas disponibles para mostrar.</p>

    <ion-list v-if="empresas.length">
      <ion-item v-for="empresa in empresas" :key="empresa.id" button detail @click="ir_a_productos(empresa.id)">
        <ion-icon slot="start" :icon="storefrontOutline" />
        <ion-label class="ion-text-wrap">
          <h2>{{ empresa.nombre_empresa }}</h2>
          <p>{{ empresa.direccion_linea || 'Sin dirección cargada' }}</p>
        </ion-label>
        <ion-badge slot="end" :color="empresa.activo ? 'success' : 'medium'">
          {{ empresa.activo ? 'Activa' : 'Inactiva' }}
        </ion-badge>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  IonBadge,
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardSubtitle,
  IonCardTitle,
  IonIcon,
  IonItem,
  IonLabel,
  IonList,
  IonText,
  onIonViewWillEnter,
} from '@ionic/vue';
import { storefrontOutline } from 'ionicons/icons';

import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import { obtener_empresas, type Empresa } from '../services/empresas_service';
import { sesion } from '../services/auth_service';

const router = useRouter();

const empresas = ref<Empresa[]>([]);
const cargando = ref(false);
const error = ref('');

const es_superadmin = computed(() => sesion.usuario.value?.rol === 'superadmin');

function mensaje(e: unknown) {
  return e && typeof e === 'object' && 'mensaje' in e
    ? String((e as { mensaje: unknown }).mensaje)
    : 'No se pudieron cargar las empresas.';
}

async function cargar() {
  cargando.value = true;
  error.value = '';
  try {
    empresas.value = await obtener_empresas();
  } catch (e) {
    empresas.value = [];
    error.value = mensaje(e);
  } finally {
    cargando.value = false;
  }
}

function ir_a_productos(id: number) {
  void router.push(`/app/empresas/${id}/productos`);
}

async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}

onIonViewWillEnter(cargar);
</script>
