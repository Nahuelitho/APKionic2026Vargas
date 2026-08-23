<template>
  <comp-page titulo="Mi cuenta">
    <ion-card>
      <ion-card-header>
        <ion-card-title>Mi cuenta</ion-card-title>
        <ion-card-subtitle>Preferencias de la app</ion-card-subtitle>
      </ion-card-header>

      <ion-card-content>
        <ion-list>
          <ion-item>
            <ion-label>
              <h3>Tema oscuro</h3>
              <p>Guardar la preferencia en este dispositivo</p>
            </ion-label>

            <ion-toggle
              :checked="tema_oscuro"
              @ionChange="cambiar_tema"
            />
          </ion-item>
        </ion-list>
      </ion-card-content>
    </ion-card>
  </comp-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';

import {
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardSubtitle,
  IonCardTitle,
  IonItem,
  IonLabel,
  IonList,
  IonToggle,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';

const CLAVE_TEMA = 'tema_oscuro';

const tema_oscuro = ref(localStorage.getItem(CLAVE_TEMA) === 'true');

function aplicar_tema(oscuro: boolean) {
  document.documentElement.classList.toggle('ion-palette-dark', oscuro);
}

function cambiar_tema(event: CustomEvent) {
  const activo = event.detail.checked;

  tema_oscuro.value = activo;
  localStorage.setItem(CLAVE_TEMA, String(activo));
  aplicar_tema(activo);
}

aplicar_tema(tema_oscuro.value);
</script>
