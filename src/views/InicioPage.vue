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
  </comp-page>
</template>

<script setup lang="ts">
import {
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardSubtitle,
  IonCardTitle,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';
</script>