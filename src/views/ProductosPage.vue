<template>
  <comp-page
    titulo="Productos"
    :mostrar_actualizar="true"
    @actualizar="actualizar"
  >
    <comp-esqueleto v-if="cargando" />

    <ion-list v-else>
      <ion-item v-for="producto in productos" :key="producto.id">
        <ion-label>
          <h3>{{ producto.nombre }}</h3>
          <p>{{ producto.descripcion }}</p>
          <p>${{ producto.precio }}</p>
        </ion-label>

        <ion-badge v-if="!producto.stock" color="warning">
          Sin stock
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
import { obtener_productos } from '../datos/productos';

type Producto = {
  id: number;
  nombre: string;
  descripcion: string;
  precio: number;
  stock: boolean;
};

const productos = ref<Producto[]>([]);
const cargando = ref(false);

async function cargar() {
  cargando.value = true;

  const respuesta = await obtener_productos() as { productos: Producto[] };
  productos.value = respuesta.productos;

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