<template>
  <comp-page titulo="Mi Empresa" :mostrar_actualizar="true" @actualizar="actualizar">
    <comp-esqueleto v-if="cargando && !empresa" />
    <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
    <ion-button v-if="error" fill="outline" @click="cargar">Reintentar</ion-button>

    <p v-if="!cargando && !error && !empresa">
      Todavía no tenés una empresa asignada. Pedile al administrador que te asigne una.
    </p>

    <template v-if="empresa">
      <h1>{{ empresa.nombre_empresa }}</h1>
      <p>Estos son los datos que ven tus clientes cuando eligen tu tienda.</p>

      <ion-button :router-link="`/app/empresas/${empresa.id}/productos`" fill="outline">
        Ver productos de mi empresa
      </ion-button>

      <comp-empresa-editor :empresa="empresa" @guardado="cargar" />
    </template>
  </comp-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { IonButton, IonText, onIonViewWillEnter } from '@ionic/vue';
import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import CompEmpresaEditor from '../components/comp-empresa-editor.vue';
import { obtener_empresas, type Empresa } from '../services/empresas_service';
import { sesion } from '../services/auth_service';

const empresa = ref<Empresa | null>(null);
const cargando = ref(false), error = ref('');

async function cargar() {
  cargando.value = true; error.value = '';
  try {
    const empresas = await obtener_empresas();
    empresa.value = empresas.find(e => e.id === sesion.usuario.value?.empresa_id) ?? empresas[0] ?? null;
    console.log('[mi-empresa] datos', empresa.value);
  } catch (e) {
    console.error('[mi-empresa] error al cargar', e);
    error.value = e && typeof e === 'object' && 'mensaje' in e ? String((e as { mensaje: unknown }).mensaje) : 'No se pudo cargar tu empresa.';
  } finally { cargando.value = false; }
}

async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}
onIonViewWillEnter(cargar);
</script>
