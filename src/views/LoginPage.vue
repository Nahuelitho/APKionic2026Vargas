<template>
  <ion-page><ion-content class="ion-padding"><div class="login-contenedor">
    <ion-card><ion-card-header><ion-card-subtitle>Gestión comercial</ion-card-subtitle><ion-card-title>Ingresar</ion-card-title></ion-card-header>
      <ion-card-content>
        <ion-input v-model="email" type="email" label="Email" label-placement="stacked" />
        <ion-input v-model="password" type="password" label="Contraseña" label-placement="stacked">
          <ion-input-password-toggle slot="end" />
        </ion-input>
        <ion-text v-if="error" color="danger"><p>{{ error }}</p></ion-text>
        <ion-button expand="block" :disabled="cargando" @click="ingresar">{{ cargando ? 'Ingresando...' : 'Ingresar' }}</ion-button>
        <p class="ayuda">Pruebas: admin@vargas.com, vendedor@vargas.com y sinrol@vargas.com.</p>
        <p class="ayuda">Contraseñas: Admin123!, Vendedor123! y SinRol123!.</p>
      </ion-card-content>
    </ion-card>
  </div></ion-content></ion-page>
</template>
<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { IonButton, IonCard, IonCardContent, IonCardHeader, IonCardSubtitle, IonCardTitle, IonContent, IonInput, IonInputPasswordToggle, IonPage, IonText } from '@ionic/vue';
import { iniciar_sesion } from '../services/auth_service';
const router = useRouter();
const email = ref(''); const password = ref(''); const error = ref(''); const cargando = ref(false);
async function ingresar() {
  error.value = ''; cargando.value = true;
  try { await iniciar_sesion(email.value, password.value); await router.replace('/app/inicio'); }
  catch (e) { error.value = e instanceof Error ? e.message : 'No se pudo ingresar.'; }
  finally { cargando.value = false; }
}
</script>
<style scoped>
.login-contenedor { min-height: 100%; display: grid; place-items: center; max-width: 460px; margin: auto; }
ion-card { width: 100%; } .ayuda { margin-top: 20px; color: var(--ion-color-medium); font-size: 0.85rem; }
</style>
