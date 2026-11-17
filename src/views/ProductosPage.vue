<template>
  <comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="actualizar">
    <section class="intro app-tarjeta">
      <h1>Elegí una empresa</h1>
      <p class="app-texto-suave">
        Los productos se muestran por empresa.
        <template v-if="!es_superadmin">Solo vas a ver la empresa a la que pertenecés.</template>
      </p>
    </section>

    <comp-lista
      :cargando="cargando"
      :error="error"
      :items="empresas.length"
      texto_vacio="No hay empresas disponibles para mostrar."
      @reintentar="cargar"
    >
      <ion-list class="lista-empresas">
        <ion-item v-for="empresa in empresas" :key="empresa.id" button detail @click="ir_a_productos(empresa.id)">
          <ion-icon slot="start" :icon="storefrontOutline" class="icono-empresa" />
          <ion-label class="ion-text-wrap">
            <div class="empresa-titulo">
              <h2>{{ empresa.nombre_empresa }}</h2>
              <comp-estado :definicion="empresa.activo ? ESTADOS_ACTIVO.activo : ESTADOS_ACTIVO.inactivo" />
            </div>
            <p>{{ empresa.direccion_linea || 'Sin dirección cargada' }}</p>
          </ion-label>
        </ion-item>
      </ion-list>
    </comp-lista>
  </comp-page>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  IonIcon,
  IonItem,
  IonLabel,
  IonList,
  onIonViewWillEnter,
} from '@ionic/vue';
import { storefrontOutline } from 'ionicons/icons';

import CompPage from '../components/base/comp-page.vue';
import CompLista from '../components/base/comp-lista.vue';
import CompEstado from '../components/base/comp-estado.vue';
import { ESTADOS_ACTIVO } from '../config/estados';
import { obtener_empresas, type Empresa } from '../services/empresas_service';
import { sesion } from '../services/auth_service';

const router = useRouter();

const empresas = ref<Empresa[]>([]);
const cargando = ref(false);
const error = ref('');

const es_superadmin = computed(() => sesion.usuario.value?.rol === 'superadmin');

function mensaje(e: unknown) {
  return e && typeof e === 'object' && 'mensaje' in e
    ? String((e as { mensaje: unknown }).mensaje)
    : 'No se pudieron cargar las empresas.';
}

async function cargar() {
  cargando.value = true;
  error.value = '';
  try {
    empresas.value = await obtener_empresas();
  } catch (e) {
    empresas.value = [];
    error.value = mensaje(e);
  } finally {
    cargando.value = false;
  }
}

function ir_a_productos(id: number) {
  void router.push(`/app/empresas/${id}/productos`);
}

async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}

onIonViewWillEnter(cargar);
</script>

<style scoped>
.intro {
  display: flex;
  flex-direction: column;
  gap: var(--app-espacio-1);
  padding: var(--app-espacio-4);
}

.intro h1 { margin: 0; font-size: var(--app-texto-lg); font-weight: 800; }
.intro p { margin: 0; }

.lista-empresas {
  margin: 0;
  border-radius: var(--app-radio-md);
  overflow: hidden;
  border: 1px solid var(--app-borde);
  box-shadow: var(--app-sombra-1);
}

.icono-empresa { color: var(--ion-color-primary); font-size: 24px; }

.empresa-titulo {
  display: flex;
  align-items: center;
  gap: var(--app-espacio-2);
  flex-wrap: wrap;
}

.empresa-titulo h2 { margin: 0; font-size: var(--app-texto-md); font-weight: 700; }
</style>
