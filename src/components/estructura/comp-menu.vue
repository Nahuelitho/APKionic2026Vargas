<template>
  <ion-menu :content-id="content_id">
    <ion-header>
      <ion-toolbar>
        <ion-title>App Multi-tenant</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content>
      <ion-list>
        <ion-list-header>Principal</ion-list-header>

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

defineProps({
  content_id: {
    type: String,
    required: true,
  },
});

const principales = navegacion_ordenada.filter((item) => item.grupo === 'principal');
const configuracion = navegacion_ordenada.filter((item) => item.grupo === 'configuracion');
</script>