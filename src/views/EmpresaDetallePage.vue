<template>
  <comp-page :titulo="empresa ? empresa.nombre_empresa : 'Empresa'">
    <ion-button fill="outline" router-link="/app/empresas">Volver a empresas</ion-button>

    <comp-esqueleto v-if="cargando && !empresa" />
    <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
    <ion-button v-if="error" fill="outline" @click="cargar">Reintentar</ion-button>

    <comp-empresa-editor v-if="empresa" :empresa="empresa" mostrar_activo @guardado="cargar" />
  </comp-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRoute } from 'vue-router';
import { IonButton, IonText, onIonViewWillEnter } from '@ionic/vue';
import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import CompEmpresaEditor from '../components/comp-empresa-editor.vue';
import { obtener_empresas, type Empresa } from '../services/empresas_service';

const route = useRoute();
const empresa = ref<Empresa | null>(null);
const cargando = ref(false), error = ref('');

async function cargar() {
  const id = Number(route.params.id);
  cargando.value = true; error.value = '';
  try {
    const lista = await obtener_empresas();
    empresa.value = lista.find(e => e.id === id) ?? null;
    if (!empresa.value) error.value = 'La empresa no existe o no tenés acceso.';
    console.log('[empresa-detalle] datos', empresa.value);
  } catch (e) {
    console.error('[empresa-detalle] error al cargar', e);
    error.value = e && typeof e === 'object' && 'mensaje' in e ? String((e as { mensaje: unknown }).mensaje) : 'No se pudo cargar la empresa.';
  } finally { cargando.value = false; }
}

onIonViewWillEnter(cargar);
</script>
