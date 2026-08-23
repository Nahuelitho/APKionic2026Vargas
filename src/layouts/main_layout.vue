<template>
  <ion-page>
    <comp-menu content_id="contenido_principal" />

    <ion-tabs id="contenido_principal">
      <ion-router-outlet />
      <comp-tabs />
    </ion-tabs>
  </ion-page>
</template>

<script setup>
import { IonPage, IonRouterOutlet, IonTabs } from '@ionic/vue';

import CompMenu from '../components/estructura/comp-menu.vue';
import CompTabs from '../components/estructura/comp-tabs.vue';
</script>