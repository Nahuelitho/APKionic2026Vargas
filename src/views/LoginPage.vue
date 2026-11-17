<template>
  <ion-page>
    <ion-content class="ion-padding">
      <div class="login-contenedor">
        <div class="marca">
          <ion-icon :icon="storefrontOutline" aria-hidden="true" />
          <span>Gestión comercial</span>
        </div>

        <h1 class="titulo">Ingresar</h1>
        <p class="app-texto-suave">Administrá tus tiendas, productos y pedidos desde un solo lugar.</p>

        <form class="formulario app-tarjeta" @submit.prevent="ingresar">
          <ion-input
            v-model="email"
            name="email"
            autocomplete="username"
            type="email"
            label="Email"
            label-placement="stacked"
            required
            helper-text="Obligatorio: es el usuario con el que te registró el administrador."
          />
          <ion-input
            v-model="password"
            name="password"
            autocomplete="current-password"
            type="password"
            label="Contraseña"
            label-placement="stacked"
            required
            helper-text="Obligatoria: se valida contra la API."
          >
            <ion-input-password-toggle slot="end" />
          </ion-input>

          <comp-aviso v-if="error" tipo="peligro" titulo="No se pudo ingresar" :mensaje="error" />

          <ion-button type="submit" expand="block" :disabled="cargando || !email.trim() || !password">
            {{ cargando ? 'Ingresando...' : 'Ingresar' }}
          </ion-button>
        </form>

        <details class="ayuda">
          <summary>Usuarios de prueba</summary>
          <p>admin@vargas.com · Admin123!</p>
          <p>vendedor@vargas.com · Vendedor123!</p>
          <p>sinrol@vargas.com · SinRol123!</p>
        </details>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  IonButton,
  IonContent,
  IonIcon,
  IonInput,
  IonInputPasswordToggle,
  IonPage,
} from '@ionic/vue';
import { storefrontOutline } from 'ionicons/icons';
import CompAviso from '../components/base/comp-aviso.vue';
import { iniciar_sesion, sesion } from '../services/auth_service';
import { hubo_navegacion_push } from '../services/push_service';
import { ruta_inicial } from '../config/navegacion';
import { vibrar_error } from '../services/vibracion_service';

const router = useRouter();
const email = ref('');
const password = ref('');
const error = ref('');
const cargando = ref(false);

async function ingresar() {
  if (cargando.value) return;
  error.value = '';
  if (!email.value.trim() || !password.value) {
    error.value = 'El email y la contraseña son obligatorios.';
    return;
  }
  cargando.value = true;
  try {
    await iniciar_sesion(email.value.trim(), password.value);
    // Si el toque de una push dejo una ruta pendiente, esa pantalla gana.
    if (hubo_navegacion_push()) return;
    await router.replace(ruta_inicial(sesion.usuario.value?.rol));
  } catch (e) {
    void vibrar_error();
    error.value = e instanceof Error ? e.message : 'No se pudo ingresar.';
  } finally {
    cargando.value = false;
  }
}
</script>

<style scoped>
.login-contenedor {
  min-height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: var(--app-espacio-2);
  max-width: 440px;
  margin: auto;
  padding-block: var(--app-espacio-5);
}

.marca {
  display: inline-flex;
  align-items: center;
  gap: var(--app-espacio-2);
  color: var(--ion-color-primary);
  font-weight: 700;
}

.marca ion-icon { font-size: 26px; }

.titulo {
  margin: 0;
  font-size: 2rem;
  font-weight: 800;
  color: var(--app-texto);
}

.formulario {
  display: flex;
  flex-direction: column;
  gap: var(--app-espacio-3);
  padding: var(--app-espacio-4);
  margin-top: var(--app-espacio-3);
}

.ayuda {
  margin-top: var(--app-espacio-3);
  color: var(--app-texto-suave);
  font-size: var(--app-texto-sm);
}

.ayuda summary { cursor: pointer; font-weight: 600; }
.ayuda p { margin: 4px 0 0; }
</style>
