<template>
  <comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-card>
      <ion-card-header>
        <ion-card-title>
          {{ producto_editando_id ? 'Modificar producto' : 'Nuevo producto' }}
        </ion-card-title>
      </ion-card-header>

      <ion-card-content>
        <ion-input
          v-model="formulario.nombre"
          label="Nombre"
          label-placement="stacked"
          placeholder="Ej: Yerba mate"
        />

        <ion-textarea
          v-model="formulario.descripcion"
          label="Descripcion"
          label-placement="stacked"
          placeholder="Detalle del producto"
        />

        <ion-input
          v-model.number="formulario.precio"
          type="number"
          label="Precio"
          label-placement="stacked"
          placeholder="0"
        />

        <ion-item lines="none">
          <ion-toggle
            :key="`${producto_editando_id || 'nuevo'}-${formulario.stock}`"
            :checked="formulario.stock"
            @ionChange="cambiar_stock"
          >
            {{ formulario.stock ? 'Con stock' : 'Sin stock' }}
          </ion-toggle>
        </ion-item>

        <p>
          Estado cargado desde API: {{ formulario.stock ? 'Con stock' : 'Sin stock' }}
        </p>

        <ion-text v-if="error_formulario" color="danger">
          <p>{{ error_formulario }}</p>
        </ion-text>

        <ion-button
          expand="block"
          :disabled="guardando"
          @click="guardar"
        >
          {{ guardando ? 'Guardando...' : producto_editando_id ? 'Guardar cambios' : 'Crear producto' }}
        </ion-button>

        <ion-button
          v-if="producto_editando_id"
          expand="block"
          fill="clear"
          color="medium"
          :disabled="guardando"
          @click="cancelar_edicion"
        >
          Cancelar edicion
        </ion-button>
      </ion-card-content>
    </ion-card>

    <comp-esqueleto v-if="cargando && !hay_productos" />

    <ion-card v-else-if="error">
      <ion-card-header>
        <ion-card-title>No se pudieron cargar los productos</ion-card-title>
      </ion-card-header>

      <ion-card-content>
        <p>{{ error }}</p>

        <ion-button expand="block" @click="cargar">
          Reintentar
        </ion-button>
      </ion-card-content>
    </ion-card>

    <ion-card v-else-if="!hay_productos">
      <ion-card-content>
        No hay productos cargados.
      </ion-card-content>
    </ion-card>

    <ion-list v-else>
      <ion-item v-for="producto in productos" :key="producto.id">
        <ion-label>
          <h3>{{ producto.nombre }}</h3>
          <p>{{ producto.descripcion || 'Sin descripcion' }}</p>
          <p>${{ producto.precio }}</p>
        </ion-label>

        <ion-badge v-if="!producto.stock" color="warning">
          Sin stock
        </ion-badge>

        <ion-button
          fill="clear"
          size="small"
          v-if="sesion.es_admin.value"
          @click="editar(producto)"
        >
          Editar
        </ion-button>

        <ion-button
          fill="clear"
          size="small"
          color="danger"
          :disabled="eliminando"
          v-if="sesion.es_admin.value"
          @click="borrar(producto.id)"
        >
          Borrar
        </ion-button>
      </ion-item>
    </ion-list>

    <ion-card v-if="sesion.es_admin.value">
      <ion-card-content>
        <p>
          Mostrando {{ productos.length }} de {{ total }} productos
        </p>

        <ion-button
          v-if="hay_mas_productos"
          expand="block"
          fill="outline"
          :disabled="cargando"
          @click="cargar_mas_productos"
        >
          {{ cargando ? 'Cargando...' : 'Ver mas' }}
        </ion-button>
      </ion-card-content>
    </ion-card>
  </comp-page>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';

import {
  IonBadge,
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardTitle,
  IonInput,
  IonItem,
  IonLabel,
  IonList,
  IonText,
  IonTextarea,
  IonToggle,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import { use_productos_store } from '../stores/productos_store';
import { obtener_producto, type Producto } from '../services/productos_service';
import { sesion } from '../services/auth_service';

const {
  productos,
  cargando,
  guardando,
  eliminando,
  error,
  error_formulario,
  total,
  hay_productos,
  hay_mas_productos,
  cargar_productos,
  cargar_mas_productos,
  crear_producto,
  actualizar_producto,
  eliminar_producto,
} = use_productos_store();

const producto_editando_id = ref<number | null>(null);

const formulario = reactive({
  nombre: '',
  descripcion: '',
  precio: 0,
  stock: true,
});

function limpiar_formulario() {
  producto_editando_id.value = null;
  formulario.nombre = '';
  formulario.descripcion = '';
  formulario.precio = 0;
  formulario.stock = true;
}

async function cargar() {
  await cargar_productos();
}

async function guardar() {
  const producto = {
    nombre: formulario.nombre,
    descripcion: formulario.descripcion || null,
    precio: Number(formulario.precio),
    stock: formulario.stock,
  };

  const guardado = producto_editando_id.value
    ? await actualizar_producto(producto_editando_id.value, producto)
    : await crear_producto(producto);

  if (guardado) {
    limpiar_formulario();
  }
}

async function editar(producto: Producto) {
  const producto_api = await obtener_producto(producto.id);

  producto_editando_id.value = producto_api.id;
  formulario.nombre = producto_api.nombre;
  formulario.descripcion = producto_api.descripcion || '';
  formulario.precio = producto_api.precio;
  formulario.stock = producto_api.stock;
}

function cancelar_edicion() {
  limpiar_formulario();
}

function cambiar_stock(event: CustomEvent) {
  formulario.stock = Boolean(event.detail.checked);
}

async function borrar(id: number) {
  const confirma = window.confirm('Seguro que queres eliminar este producto?');

  if (!confirma) {
    return;
  }

  await eliminar_producto(id);

  if (producto_editando_id.value === id) {
    limpiar_formulario();
  }
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
