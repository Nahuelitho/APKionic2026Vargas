<template>
  <ion-tab-bar slot="bottom">
    <ion-tab-button
      v-for="item in tabs"
      :key="item.id"
      :tab="item.id"
      :href="item.ruta"
    >
      <ion-icon :icon="item.icono" />
      <ion-label>{{ item.titulo }}</ion-label>
    </ion-tab-button>
  </ion-tab-bar>
</template>

<script setup>
import {
  IonIcon,
  IonLabel,
  IonTabBar,
  IonTabButton,
} from '@ionic/vue';

import { navegacion_ordenada } from '../../config/navegacion';

const tabs = navegacion_ordenada.filter((item) => item.tab);
</script>