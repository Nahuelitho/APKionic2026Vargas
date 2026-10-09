<template>
  <comp-page titulo="Empresas" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-card><ion-card-content>
      <ion-input v-model="nombre" label="Nombre de la empresa" label-placement="stacked" :maxlength="160" />
      <ion-button :disabled="guardando || !nombre.trim()" @click="crear">Crear empresa</ion-button>
    </ion-card-content></ion-card>
    <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
    <ion-list>
      <ion-item v-for="empresa in empresas" :key="empresa.id">
        <ion-input v-model="empresa.nombre_empresa" :label="`Empresa ${empresa.id}`" label-placement="stacked" :maxlength="160" />
        <ion-toggle v-model="empresa.activo">Activa</ion-toggle>
        <ion-button :disabled="guardando" @click="guardar(empresa)">Guardar</ion-button>
      </ion-item>
    </ion-list>
    <p>Desactivar una empresa bloquea sus membresias y su catalogo publico; conserva sus pedidos.</p>
  </comp-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { IonButton, IonCard, IonCardContent, IonInput, IonItem, IonList, IonText, IonToggle, onIonViewWillEnter } from '@ionic/vue';
import CompPage from '../components/base/comp-page.vue';
import { actualizar_empresa, crear_empresa, obtener_empresas, type Empresa } from '../services/empresas_service';

const empresas = ref<Empresa[]>([]), nombre = ref(''), error = ref(''), guardando = ref(false);
const mensaje = (e: unknown) => e && typeof e === 'object' && 'mensaje' in e ? String(e.mensaje) : 'No se pudo guardar la empresa.';
async function cargar() { try { empresas.value = await obtener_empresas(); } catch (e) { error.value = mensaje(e); } }
async function crear() {
  guardando.value = true; error.value = '';
  try { await crear_empresa(nombre.value.trim()); nombre.value = ''; await cargar(); }
  catch (e) { error.value = mensaje(e); } finally { guardando.value = false; }
}
async function guardar(empresa: Empresa) {
  guardando.value = true; error.value = '';
  try { await actualizar_empresa(empresa); await cargar(); }
  catch (e) { error.value = mensaje(e); } finally { guardando.value = false; }
}
async function actualizar(event: CustomEvent) { try { await cargar(); } finally { await (event.target as HTMLIonRefresherElement).complete(); } }
onIonViewWillEnter(() => { empresas.value = []; void cargar(); });
</script>
