<template>
  <ion-menu :content-id="content_id">
    <ion-header>
      <ion-toolbar>
        <ion-title>App Multi-tenant</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content>
      <ion-list>
        <ion-list-header>{{ sesion.usuario.value?.nombre }}</ion-list-header>

        <ion-menu-toggle
          v-for="item in principales"
          :key="item.id"
          auto-hide="true"
        >
          <ion-item
            button
            detail="false"
            :router-link="item.ruta"
            router-direction="root"
          >
            <ion-icon slot="start" :icon="item.icono" />
            <ion-label>{{ item.titulo }}</ion-label>
          </ion-item>
        </ion-menu-toggle>
      </ion-list>

      <ion-list>
        <ion-list-header>Configuracion</ion-list-header>

        <ion-menu-toggle
          v-for="item in configuracion"
          :key="item.id"
          auto-hide="true"
        >
          <ion-item
            button
            detail="false"
            :router-link="item.ruta"
            router-direction="root"
          >
            <ion-icon slot="start" :icon="item.icono" />
            <ion-label>{{ item.titulo }}</ion-label>
          </ion-item>
        </ion-menu-toggle>
      </ion-list>
    </ion-content>
  </ion-menu>
</template>

<script setup>
import {
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonLabel,
  IonList,
  IonListHeader,
  IonMenu,
  IonMenuToggle,
  IonTitle,
  IonToolbar,
} from '@ionic/vue';

import { navegacion_ordenada } from '../../config/navegacion';
import { computed } from 'vue';
import { sesion } from '../../services/auth_service';

defineProps({
  content_id: {
    type: String,
    required: true,
  },
});

const permitidos = computed(() => navegacion_ordenada.filter((item) => !item.roles || item.roles.includes(sesion.usuario.value?.rol || '')));
const principales = computed(() => permitidos.value.filter((item) => item.grupo === 'principal'));
const configuracion = computed(() => permitidos.value.filter((item) => item.grupo === 'configuracion'));
</script>
