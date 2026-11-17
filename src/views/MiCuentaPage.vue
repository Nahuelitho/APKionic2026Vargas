<template>
  <comp-page titulo="Mi cuenta">
    <section class="perfil app-tarjeta">
      <div class="avatar" aria-hidden="true">
        <ion-icon :icon="personCircleOutline" />
      </div>
      <div class="perfil-datos">
        <h1>{{ sesion.usuario.value?.nombre || 'Mi cuenta' }}</h1>
        <p>{{ sesion.usuario.value?.email }}</p>
        <div class="app-acciones">
          <ion-badge color="primary">{{ sesion.usuario.value?.rol || 'Usuario común' }}</ion-badge>
          <ion-badge v-if="sesion.usuario.value?.empresa_id" color="medium">
            Empresa {{ sesion.usuario.value.empresa_id }}
          </ion-badge>
        </div>
      </div>
    </section>

    <comp-aviso
      v-if="!sesion.usuario.value?.rol"
      tipo="info"
      titulo="Cuenta de usuario común"
      mensaje="Podés consultar las tiendas y sus productos, crear pedidos y ver solamente tus propios pedidos."
    />

    <section class="bloque">
      <h2 class="app-titulo-seccion">Preferencias</h2>
      <ion-list class="lista-ajustes">
        <ion-item>
          <ion-label>
            <h3>Tema oscuro</h3>
            <p>Guardar la preferencia en este dispositivo</p>
          </ion-label>
          <ion-toggle :checked="tema_oscuro" @ionChange="cambiar_tema" />
        </ion-item>
        <ion-item>
          <ion-label>
            <h3>Vibración</h3>
            <p>Avisar con vibración cuando ocurre un error</p>
          </ion-label>
          <ion-toggle :checked="vibracion" @ionChange="cambiar_vibracion" />
        </ion-item>
        <ion-item>
          <ion-label>
            <h3>Desbloqueo facial</h3>
            <p>Usá el reconocimiento facial registrado en el teléfono</p>
          </ion-label>
          <ion-toggle :checked="biometria" @ionChange="cambiar_biometria" />
        </ion-item>
      </ion-list>
      <comp-aviso v-if="error_biometria" tipo="peligro" :mensaje="error_biometria" />
    </section>

    <section class="bloque">
      <h2 class="app-titulo-seccion">Notificaciones</h2>
      <p class="app-texto-suave">
        Al entrar, este teléfono se registra para recibir las push de tus pedidos.
        Al cerrar sesión se da de baja: no llega nada más en este dispositivo.
      </p>
      <comp-aviso
        tipo="info"
        titulo="Push de pedidos"
        mensaje="Las push abren directamente el pedido que cambió. También quedan en la campanita."
      />
    </section>

    <ion-button expand="block" color="danger" fill="outline" @click="salir">
      <ion-icon slot="start" :icon="logOutOutline" />
      Cerrar sesión
    </ion-button>
  </comp-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  IonBadge,
  IonButton,
  IonIcon,
  IonItem,
  IonLabel,
  IonList,
  IonToggle,
} from '@ionic/vue';
import { logOutOutline, personCircleOutline } from 'ionicons/icons';

import CompPage from '../components/base/comp-page.vue';
import CompAviso from '../components/base/comp-aviso.vue';
import { cerrar_sesion, configurar_biometria, sesion } from '../services/auth_service';
import { configurar_vibracion, vibracion_activada, vibrar_error } from '../services/vibracion_service';

const CLAVE_TEMA = 'tema_oscuro';

const tema_oscuro = ref(localStorage.getItem(CLAVE_TEMA) === 'true');
const router = useRouter();
const biometria = ref(sesion.biometria_activa.value);
const error_biometria = ref('');
const vibracion = ref(vibracion_activada());
const saliendo = ref(false);

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
  if (saliendo.value) return;
  if (!window.confirm('¿Cerrar sesión? Este teléfono dejará de recibir notificaciones push.')) return;
  saliendo.value = true;
  try {
    await cerrar_sesion();
    await router.replace('/login');
  } finally {
    saliendo.value = false;
  }
}

aplicar_tema(tema_oscuro.value);
</script>

<style scoped>
.perfil {
  display: flex;
  align-items: center;
  gap: var(--app-espacio-3);
  padding: var(--app-espacio-4);
}

.avatar ion-icon {
  font-size: 52px;
  color: var(--ion-color-primary);
}

.perfil-datos { min-width: 0; }

.perfil-datos h1 {
  margin: 0;
  font-size: var(--app-texto-lg);
  font-weight: 800;
  color: var(--app-texto);
}

.perfil-datos p {
  margin: 2px 0 var(--app-espacio-2);
  color: var(--app-texto-suave);
  font-size: var(--app-texto-sm);
  word-break: break-all;
}

.bloque {
  display: flex;
  flex-direction: column;
  gap: var(--app-espacio-2);
}

.bloque p { margin: 0; }

.lista-ajustes {
  margin: 0;
  border-radius: var(--app-radio-md);
  overflow: hidden;
  border: 1px solid var(--app-borde);
  box-shadow: var(--app-sombra-1);
}

.lista-ajustes h3 { font-size: var(--app-texto-md); font-weight: 600; }
.lista-ajustes p { font-size: var(--app-texto-xs); color: var(--app-texto-suave); }
</style>
