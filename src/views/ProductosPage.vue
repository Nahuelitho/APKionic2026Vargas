<template>
  <comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="actualizar">
    <comp-esqueleto v-if="cargando" />

    <ion-card v-else-if="error">
      <ion-card-header>
        <ion-card-title>No se pudieron cargar los productos</ion-card-title>
      </ion-card-header>

      <ion-card-content>
        <p>{{ error }}</p>

        <ion-button expand="block" @click="cargar">
          Reintentar
        </ion-button>
      </ion-card-content>
    </ion-card>

    <ion-card v-else-if="!hay_productos">
      <ion-card-content>
        No hay productos cargados.
      </ion-card-content>
    </ion-card>

    <ion-list v-else>
      <ion-item v-for="producto in productos" :key="producto.id">
        <ion-label>
          <h3>{{ producto.nombre }}</h3>
          <p>{{ producto.descripcion }}</p>
          <p>${{ producto.precio }}</p>
        </ion-label>

        <ion-badge v-if="!producto.stock" color="warning">
          Sin stock
        </ion-badge>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';

import {
  IonBadge,
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardTitle,
  IonItem,
  IonLabel,
  IonList,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import { use_productos_store } from '../stores/productos_store';

const {
  productos,
  cargando,
  error,
  hay_productos,
  cargar_productos,
} = use_productos_store();

async function cargar() {
  await cargar_productos();
}

async function actualizar(event: CustomEvent) {
  await cargar();

  const refresher = event.target as HTMLIonRefresherElement;
  refresher.complete();
}

onMounted(() => {
  cargar();
});

</script>