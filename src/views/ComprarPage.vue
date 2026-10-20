<template>
  <comp-page titulo="Elegir empresa" :mostrar_actualizar="true" @actualizar="actualizar">
    <div class="ion-padding">
      <h1>Donde queres comprar?</h1>
      <p>Elegi una empresa para ver sus productos y preparar tu pedido.</p>
      <comp-esqueleto v-if="cargando && !empresas.length" />
      <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
      <ion-button v-if="error" fill="outline" @click="cargar">Reintentar</ion-button>
      <p v-if="!cargando && !error && !empresas.length">No hay empresas disponibles por el momento.</p>
      <ion-list v-if="empresas.length">
        <ion-item v-for="empresa in empresas" :key="empresa.id" button :detail="true" :router-link="`/app/catalogo/${empresa.id}`">
          <ion-icon slot="start" :icon="storefrontOutline" />
          <ion-label class="ion-text-wrap"><h2>{{ empresa.nombre_empresa }}</h2><p>Ver productos y comprar</p></ion-label>
        </ion-item>
      </ion-list>
    </div>
  </comp-page>
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref } from 'vue';
import { IonButton, IonIcon, IonItem, IonLabel, IonList, IonText, onIonViewWillEnter, onIonViewWillLeave } from '@ionic/vue';
import { storefrontOutline } from 'ionicons/icons';
import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import { obtener_empresas, type Empresa } from '../services/empresas_service';

const empresas = ref<Empresa[]>([]), cargando = ref(false), error = ref('');
let version = 0;
async function cargar() {
  const actual = ++version;
  cargando.value = true; error.value = '';
  try {
    const respuesta = await obtener_empresas();
    if (actual === version) empresas.value = respuesta.filter(e => e.activo);
  } catch (causa) {
    if (actual === version) error.value = causa && typeof causa === 'object' && 'mensaje' in causa ? String(causa.mensaje) : 'No se pudieron cargar las empresas.';
  } finally { if (actual === version) cargando.value = false; }
}
async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}
function salir() { ++version; empresas.value = []; cargando.value = false; }
onIonViewWillEnter(cargar);
onIonViewWillLeave(salir);
onBeforeUnmount(salir);
</script>
