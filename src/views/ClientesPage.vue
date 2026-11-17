<template>
  <comp-page titulo="Clientes" :mostrar_actualizar="true" @actualizar="actualizar">
    <comp-lista
      :cargando="cargando"
      :error="error"
      :items="clientes.length"
      texto_vacio="No hay clientes cargados todavía."
      @reintentar="cargar"
    >
      <ion-list class="lista-clientes">
        <ion-item v-for="cliente in clientes" :key="cliente.id">
          <ion-icon slot="start" :icon="personOutline" class="icono-cliente" />
          <ion-label class="ion-text-wrap">
            <h3>{{ cliente.nombre }}</h3>
            <p>{{ cliente.telefono }}</p>
            <p>{{ cliente.direccion }}</p>
          </ion-label>
        </ion-item>
      </ion-list>
    </comp-lista>
  </comp-page>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { IonIcon, IonItem, IonLabel, IonList } from '@ionic/vue';
import { personOutline } from 'ionicons/icons';

import CompPage from '../components/base/comp-page.vue';
import CompLista from '../components/base/comp-lista.vue';
import { obtener_clientes } from '../datos/clientes';

type Cliente = {
  id: number;
  nombre: string;
  telefono: string;
  direccion: string;
};

const clientes = ref<Cliente[]>([]);
const cargando = ref(false);
const error = ref('');

async function cargar() {
  cargando.value = true;
  error.value = '';
  try {
    const respuesta = await obtener_clientes() as { clientes: Cliente[] };
    clientes.value = respuesta.clientes;
  } catch (e) {
    clientes.value = [];
    error.value = e && typeof e === 'object' && 'mensaje' in e
      ? String((e as { mensaje: unknown }).mensaje)
      : 'No se pudieron cargar los clientes.';
  } finally {
    cargando.value = false;
  }
}

async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}

onMounted(cargar);
</script>

<style scoped>
.lista-clientes {
  margin: 0;
  border-radius: var(--app-radio-md);
  overflow: hidden;
  border: 1px solid var(--app-borde);
  box-shadow: var(--app-sombra-1);
}

.icono-cliente { color: var(--ion-color-primary); font-size: 24px; }
.lista-clientes h3 { font-size: var(--app-texto-md); font-weight: 700; }
</style>
