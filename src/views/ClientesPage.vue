<template>
  <comp-page
    titulo="Clientes"
    :mostrar_actualizar="true"
    @actualizar="actualizar"
  >
    <comp-esqueleto v-if="cargando" />

    <ion-list v-else>
      <ion-item v-for="cliente in clientes" :key="cliente.id">
        <ion-label>
          <h3>{{ cliente.nombre }}</h3>
          <p>{{ cliente.telefono }}</p>
          <p>{{ cliente.direccion }}</p>
        </ion-label>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';

import {
  IonItem,
  IonLabel,
  IonList,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import { obtener_clientes } from '../datos/clientes';

type Cliente = {
  id: number;
  nombre: string;
  telefono: string;
  direccion: string;
};

const clientes = ref<Cliente[]>([]);
const cargando = ref(false);

async function cargar() {
  cargando.value = true;

  const respuesta = await obtener_clientes() as { clientes: Cliente[] };
  clientes.value = respuesta.clientes;

  cargando.value = false;
}

async function actualizar(event: CustomEvent) {
  await cargar();

  const refresher = event.target as HTMLIonRefresherElement;
  refresher.complete();
}

onMounted(() => {
  cargar();
});
</script>