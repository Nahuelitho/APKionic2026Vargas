<template>
  <comp-page
    titulo="Pedidos"
    :mostrar_actualizar="true"
    @actualizar="actualizar"
  >
    <comp-esqueleto v-if="cargando" />

    <ion-list v-else>
      <ion-item v-for="pedido in pedidos" :key="pedido.id">
        <ion-label>
          <h3>{{ pedido.codigo }}</h3>
          <p>{{ pedido.cliente }}</p>
          <p>${{ pedido.total }}</p>
        </ion-label>

        <ion-badge :color="pedido.color">
          {{ pedido.estado }}
        </ion-badge>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';

import {
  IonBadge,
  IonItem,
  IonLabel,
  IonList,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import { obtener_pedidos } from '../datos/pedidos';

type Pedido = {
  id: number;
  codigo: string;
  cliente: string;
  total: number;
  estado: string;
  color: string;
};

const pedidos = ref<Pedido[]>([]);
const cargando = ref(false);

async function cargar() {
  cargando.value = true;

  const respuesta = await obtener_pedidos() as { pedidos: Pedido[] };
  pedidos.value = respuesta.pedidos;

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