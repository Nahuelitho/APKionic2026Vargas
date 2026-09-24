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
import { computed } from 'vue';
import { sesion } from '../../services/auth_service';

const tabs = computed(() => navegacion_ordenada.filter((item) => {
  if (!item.tab) return false;
  const roles = item.tab_roles ?? item.roles;
  return !roles || roles.includes(sesion.usuario.value?.rol || '');
}));
</script>
