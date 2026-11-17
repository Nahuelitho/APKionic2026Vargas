<template>
  <comp-page titulo="Mi Empresa" :mostrar_actualizar="true" @actualizar="actualizar">
    <comp-esqueleto v-if="cargando && !empresa" :filas="2" />
    <comp-aviso v-if="error" tipo="peligro" :mensaje="error" accion="Reintentar" @accion="cargar" />

    <comp-aviso
      v-if="!cargando && !error && !empresa"
      tipo="advertencia"
      mensaje="Todavía no tenés una empresa asignada. Pedile al administrador que te asigne una."
    />

    <template v-if="empresa">
      <header class="mi-empresa-cabecera">
        <h1>{{ empresa.nombre_empresa }}</h1>
        <p class="app-texto-suave">Estos son los datos que ven tus clientes cuando eligen tu tienda.</p>
      </header>

      <ion-button :router-link="`/app/empresas/${empresa.id}/productos`" fill="outline">
        Ver productos de mi empresa
      </ion-button>

      <comp-empresa-editor :empresa="empresa" @guardado="cargar" />
    </template>
  </comp-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { IonButton, onIonViewWillEnter } from '@ionic/vue';
import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import CompAviso from '../components/base/comp-aviso.vue';
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

<style scoped>
.mi-empresa-cabecera h1 { margin: 0; font-size: var(--app-texto-xl); font-weight: 800; }
.mi-empresa-cabecera p { margin: 2px 0 var(--app-espacio-2); }
</style>
