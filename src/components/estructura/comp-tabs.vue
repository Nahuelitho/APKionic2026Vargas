<template>
  <ion-tab-bar slot="bottom">
    <ion-tab-button
      v-for="item in tabs"
      :key="item.id"
      :tab="item.id"
      :href="item.ruta"
    >
      <ion-icon :icon="item.icono" />
      <ion-label>{{ titulo_item(item, sesion.usuario.value?.rol) }}</ion-label>
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

import { navegacion_ordenada, puede_ver_item, titulo_item } from '../../config/navegacion';
import { computed } from 'vue';
import { sesion } from '../../services/auth_service';
import { use_offline_store } from '../../stores/offline_store';

const offline_store = use_offline_store();
const tabs = computed(() => navegacion_ordenada.filter((item) => {
  if (!item.tab || !puede_ver_item(item, sesion.usuario.value?.rol, offline_store.sin_conexion.value)) return false;
  const roles = item.tab_roles ?? item.roles;
  return !roles || roles.includes(sesion.usuario.value?.rol || '');
}));
</script>
