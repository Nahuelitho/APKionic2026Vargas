<template>
  <comp-page titulo="Mi cuenta">
    <ion-card>
      <ion-card-header>
        <ion-card-title>Mi cuenta</ion-card-title>
        <ion-card-subtitle>Preferencias de la app</ion-card-subtitle>
      </ion-card-header>

      <ion-card-content>
        <ion-text v-if="!sesion.usuario.value?.rol" color="warning">
          <p>Cuenta de usuario comun: podes consultar las empresas y sus productos, crear pedidos y ver solamente tus propios pedidos.</p>
        </ion-text>
        <ion-list>
          <ion-item>
            <ion-label><h3>{{ sesion.usuario.value?.nombre }}</h3><p>{{ sesion.usuario.value?.email }} · {{ sesion.usuario.value?.rol || 'Usuario comun' }}</p><p v-if="sesion.usuario.value?.empresa_id">Empresa {{ sesion.usuario.value.empresa_id }}</p></ion-label>
          </ion-item>
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
          <ion-item>
            <ion-label><h3>Vibración</h3><p>Avisar con vibración cuando ocurre un error</p></ion-label>
            <ion-toggle :checked="vibracion" @ionChange="cambiar_vibracion" />
          </ion-item>
          <ion-item>
            <ion-label><h3>Desbloqueo facial</h3><p>Usá el reconocimiento facial registrado en el teléfono</p></ion-label>
            <ion-toggle :checked="biometria" @ionChange="cambiar_biometria" />
          </ion-item>
        </ion-list>
        <ion-text v-if="error_biometria" color="danger"><p>{{ error_biometria }}</p></ion-text>
        <ion-button expand="block" color="danger" fill="outline" @click="salir">Cerrar sesión</ion-button>
      </ion-card-content>
    </ion-card>
  </comp-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';

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
  IonButton,
  IonText,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';
import { cerrar_sesion, configurar_biometria, sesion } from '../services/auth_service';
import { configurar_vibracion, vibracion_activada, vibrar_error } from '../services/vibracion_service';

const CLAVE_TEMA = 'tema_oscuro';

const tema_oscuro = ref(localStorage.getItem(CLAVE_TEMA) === 'true');
const router = useRouter();
const biometria = ref(sesion.biometria_activa.value);
const error_biometria = ref('');
const vibracion = ref(vibracion_activada());

function cambiar_vibracion(event: CustomEvent) {
  vibracion.value = Boolean(event.detail.checked);
  configurar_vibracion(vibracion.value);
  if (vibracion.value) void vibrar_error();
}

function aplicar_tema(oscuro: boolean) {
  document.documentElement.classList.toggle('ion-palette-dark', oscuro);
}

function cambiar_tema(event: CustomEvent) {
  const activo = event.detail.checked;

  tema_oscuro.value = activo;
  localStorage.setItem(CLAVE_TEMA, String(activo));
  aplicar_tema(activo);
}

async function cambiar_biometria(event: CustomEvent) {
  const activar = Boolean(event.detail.checked);
  error_biometria.value = '';
  try {
    await configurar_biometria(activar);
    biometria.value = activar;
  } catch (e) {
    biometria.value = false;
    error_biometria.value = e instanceof Error ? e.message : 'No se pudo configurar el reconocimiento facial.';
  }
}

async function salir() {
  await cerrar_sesion();
  await router.replace('/login');
}

aplicar_tema(tema_oscuro.value);
</script>
