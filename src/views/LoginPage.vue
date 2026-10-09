<template>
  <ion-page><ion-content class="ion-padding"><div class="login-contenedor">
    <ion-card><ion-card-header><ion-card-subtitle>Gestión comercial</ion-card-subtitle><ion-card-title>Ingresar</ion-card-title></ion-card-header>
      <ion-card-content>
        <form @submit.prevent="ingresar">
        <ion-input v-model="email" name="email" autocomplete="username" type="email" label="Email" label-placement="stacked" required />
        <ion-input v-model="password" name="password" autocomplete="current-password" type="password" label="Contraseña" label-placement="stacked" required>
          <ion-input-password-toggle slot="end" />
        </ion-input>
        <ion-input v-model.number="empresa_id" type="number" label="ID de empresa (opcional para membresias multiples)" label-placement="stacked" min="1" />
        <ion-text v-if="error" color="danger"><p>{{ error }}</p></ion-text>
        <ion-button type="submit" expand="block" :disabled="cargando">{{ cargando ? 'Ingresando...' : 'Ingresar' }}</ion-button>
        </form>
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
import { vibrar_error } from '../services/vibracion_service';
const router = useRouter();
const email = ref(''); const password = ref(''); const error = ref(''); const cargando = ref(false);
const empresa_id = ref<number | undefined>();
async function ingresar() {
  if (cargando.value) return;
  error.value = ''; cargando.value = true;
   try { await iniciar_sesion(email.value, password.value, empresa_id.value || undefined); await router.replace('/app/inicio'); }
  catch (e) { void vibrar_error(); error.value = e instanceof Error ? e.message : 'No se pudo ingresar.'; }
  finally { cargando.value = false; }
}
</script>
<style scoped>
.login-contenedor { min-height: 100%; display: grid; place-items: center; max-width: 460px; margin: auto; }
ion-card { width: 100%; } .ayuda { margin-top: 20px; color: var(--ion-color-medium); font-size: 0.85rem; }
</style>
