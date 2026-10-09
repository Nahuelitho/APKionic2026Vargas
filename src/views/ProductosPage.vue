<template>
  <comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-card v-if="puede_gestionar_productos">
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

        <div class="foto-formulario">
          <p>Foto del producto (JPEG, PNG o WebP, hasta 5 MB)</p>
          <img
            v-if="foto_preview"
            :src="foto_preview"
            alt="Vista previa de la foto del producto"
            class="foto-preview"
          />
          <comp-producto-foto v-else-if="foto_actual_url" :foto-url="foto_actual_url" alt="Foto actual del producto" class="foto-preview" />
          <div class="foto-acciones">
            <template v-if="es_nativo">
              <ion-button fill="outline" :disabled="guardando || seleccionando_foto || cargando_edicion" @click="seleccionar_foto('camara')">
                Camara
              </ion-button>
              <ion-button fill="outline" :disabled="guardando || seleccionando_foto || cargando_edicion" @click="seleccionar_foto('galeria')">
                Galeria
              </ion-button>
            </template>
            <ion-button v-else fill="outline" :disabled="guardando || cargando_edicion" @click="selector_foto?.click()">
              Seleccionar foto
            </ion-button>
            <ion-button v-if="foto_archivo" fill="clear" color="medium" :disabled="guardando" @click="descartar_foto">
              Descartar foto nueva
            </ion-button>
          </div>
          <input
            ref="selector_foto"
            type="file"
            accept="image/jpeg,image/png,image/webp"
            hidden
            @change="seleccionar_archivo"
          />
          <p v-if="seleccionando_foto">Obteniendo foto...</p>
          <ion-text v-if="error_foto" color="danger"><p>{{ error_foto }}</p></ion-text>
        </div>

        <ion-text v-if="error_formulario" color="danger">
          <p>{{ error_formulario }}</p>
        </ion-text>

        <ion-button
          expand="block"
          :disabled="guardando || seleccionando_foto || cargando_edicion"
          @click="guardar"
        >
          {{ guardando ? 'Guardando...' : producto_editando_id ? 'Guardar cambios' : 'Crear producto' }}
        </ion-button>

        <ion-button
          v-if="producto_editando_id || foto_archivo || cargando_edicion"
          expand="block"
          fill="clear"
          color="medium"
          :disabled="guardando"
          @click="cancelar_edicion"
        >
          {{ producto_editando_id || cargando_edicion ? 'Cancelar edicion' : 'Limpiar formulario' }}
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

    <ion-card v-else-if="!hay_productos && !empresas.length">
      <ion-card-content>
        No hay productos cargados.
      </ion-card-content>
    </ion-card>

    <ion-list v-else>
      <template v-for="grupo in grupos" :key="grupo.id">
      <ion-item-divider><ion-label>{{ grupo.nombre }}</ion-label></ion-item-divider>
      <ion-item v-if="!grupo.productos.length"><ion-label>Sin productos en esta pagina.</ion-label></ion-item>
      <ion-item v-for="producto in grupo.productos" :key="producto.id">
        <ion-thumbnail v-if="producto.fotoUrl" slot="start">
          <comp-producto-foto :foto-url="producto.fotoUrl" :alt="`Foto de ${producto.nombre}`" />
        </ion-thumbnail>
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
           v-if="puede_gestionar_productos"
          :disabled="guardando || seleccionando_foto || cargando_edicion"
          @click="editar(producto)"
        >
          Editar
        </ion-button>

        <ion-button
          fill="clear"
          size="small"
          color="danger"
          :disabled="eliminando"
           v-if="puede_gestionar_productos"
          @click="borrar(producto.id)"
        >
          Borrar
        </ion-button>
      </ion-item>
      </template>
    </ion-list>

     <ion-card>
      <ion-card-content>
        <ion-select v-if="sesion.usuario.value?.rol === 'superadmin'" v-model="empresa_id" label="Empresa" label-placement="stacked" :disabled="!!producto_editando_id">
          <ion-select-option v-for="empresa in empresas.filter(e => e.activo)" :key="empresa.id" :value="empresa.id">{{ empresa.nombre_empresa }}</ion-select-option>
        </ion-select>
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
import { onBeforeUnmount, reactive, ref, watch } from 'vue';
import { computed } from 'vue';
import { Capacitor } from '@capacitor/core';

import {
  IonBadge,
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardTitle,
  IonInput,
  IonItem,
  IonItemDivider,
  IonLabel,
  IonList,
  IonSelect,
  IonSelectOption,
  IonText,
  IonTextarea,
  IonToggle,
  IonThumbnail,
  onIonViewDidLeave,
  onIonViewWillEnter,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import CompProductoFoto from '../components/comp-producto-foto.vue';
import { use_productos_store } from '../stores/productos_store';
import { obtener_producto, type Producto } from '../services/productos_service';
import { obtener_foto, validar_foto } from '../services/camara_service';
import { sesion } from '../services/auth_service';
import { obtener_empresas, type Empresa } from '../services/empresas_service';

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

const puede_gestionar_productos = computed(() => ['superadmin', 'administrador', 'vendedor'].includes(sesion.usuario.value?.rol || ''));
const empresas = ref<Empresa[]>([]), empresa_id = ref<number | null>(null);
const grupos = computed(() => Array.from(new Set([...empresas.value.map(e => e.id), ...productos.value.map(p => p.empresa_id)])).map(id => ({
  id, nombre: empresas.value.find(e => e.id === id)?.nombre_empresa ?? productos.value.find(p => p.empresa_id === id)!.nombre_empresa,
  productos: productos.value.filter(p => p.empresa_id === id),
})));

const producto_editando_id = ref<number | null>(null);
const es_nativo = Capacitor.isNativePlatform();
const selector_foto = ref<HTMLInputElement | null>(null);
const foto_archivo = ref<File | null>(null);
const foto_preview = ref('');
const foto_actual_url = ref('');
const error_foto = ref<string | null>(null);
const seleccionando_foto = ref(false);
const cargando_edicion = ref(false);
let version_formulario = 0;

const formulario = reactive({
  nombre: '',
  descripcion: '',
  precio: 0,
  stock: true,
});

function limpiar_formulario() {
  version_formulario += 1;
  descartar_foto();
  foto_actual_url.value = '';
  seleccionando_foto.value = false;
  cargando_edicion.value = false;
  error_formulario.value = null;
  producto_editando_id.value = null;
  formulario.nombre = '';
  formulario.descripcion = '';
  formulario.precio = 0;
  formulario.stock = true;
}

function descartar_foto() {
  if (foto_preview.value) URL.revokeObjectURL(foto_preview.value);
  foto_preview.value = '';
  foto_archivo.value = null;
  error_foto.value = null;
  if (selector_foto.value) selector_foto.value.value = '';
}

function usar_archivo(archivo: File) {
  const mensaje = validar_foto(archivo);
  if (mensaje) {
    error_foto.value = mensaje;
    return;
  }
  descartar_foto();
  foto_archivo.value = archivo;
  foto_preview.value = URL.createObjectURL(archivo);
}

function seleccionar_archivo(event: Event) {
  const input = event.target as HTMLInputElement;
  const archivo = input.files?.[0];
  if (archivo && puede_gestionar_productos.value && !guardando.value && !cargando_edicion.value) usar_archivo(archivo);
  input.value = '';
}

async function seleccionar_foto(origen: 'camara' | 'galeria') {
  if (!puede_gestionar_productos.value || guardando.value || seleccionando_foto.value || cargando_edicion.value) return;
  const version = version_formulario;
  seleccionando_foto.value = true;
  error_foto.value = null;
  const resultado = await obtener_foto(origen);
  if (version !== version_formulario) return;
  seleccionando_foto.value = false;
  if (resultado.ok && resultado.archivo) usar_archivo(resultado.archivo);
  else error_foto.value = resultado.mensaje;
}

async function cargar() {
  await cargar_productos();
}

async function guardar() {
  if (!puede_gestionar_productos.value || guardando.value || seleccionando_foto.value || cargando_edicion.value) return;
  if (foto_archivo.value) {
    error_foto.value = validar_foto(foto_archivo.value);
    if (error_foto.value) return;
  }
  const version = version_formulario;
  const producto = {
    nombre: formulario.nombre,
    descripcion: formulario.descripcion || null,
    precio: Number(formulario.precio),
    stock: formulario.stock,
    foto: foto_archivo.value,
    empresa_id: empresa_id.value ?? sesion.usuario.value?.empresa_id,
  };

  const guardado = producto_editando_id.value
    ? await actualizar_producto(producto_editando_id.value, producto)
    : await crear_producto(producto);

  if (guardado && version === version_formulario) {
    limpiar_formulario();
  }
}

async function editar(producto: Producto) {
  if (!puede_gestionar_productos.value || guardando.value || seleccionando_foto.value || cargando_edicion.value) return;
  limpiar_formulario();
  const version = version_formulario;
  cargando_edicion.value = true;
  try {
    const producto_api = await obtener_producto(producto.id);
    if (version !== version_formulario) return;
    producto_editando_id.value = producto_api.id;
    empresa_id.value = producto_api.empresa_id;
    formulario.nombre = producto_api.nombre;
    formulario.descripcion = producto_api.descripcion || '';
    formulario.precio = producto_api.precio;
    formulario.stock = producto_api.stock;
    foto_actual_url.value = producto_api.fotoUrl || '';
  } catch (excepcion) {
    if (version === version_formulario) {
      const ajax_error = excepcion as { mensaje?: string };
      error_formulario.value = ajax_error.mensaje || 'No se pudo cargar el producto para editar.';
    }
  } finally {
    if (version === version_formulario) cargando_edicion.value = false;
  }
}

function cancelar_edicion() {
  limpiar_formulario();
}

function cambiar_stock(event: CustomEvent) {
  formulario.stock = Boolean(event.detail.checked);
}

async function borrar(id: number) {
  if (!puede_gestionar_productos.value || eliminando.value) return;
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

onIonViewWillEnter(async () => {
  await cargar();
  try { empresas.value = await obtener_empresas(); empresa_id.value = empresas.value.find(e => e.activo)?.id ?? null; }
  catch { error_formulario.value = 'No se pudieron cargar las empresas.'; }
});

watch(puede_gestionar_productos, (puede_gestionar) => {
  if (!puede_gestionar) limpiar_formulario();
});

// Ionic puede conservar la vista montada al navegar.
onIonViewDidLeave(limpiar_formulario);
onBeforeUnmount(limpiar_formulario);
</script>

<style scoped>
.foto-preview {
  display: block;
  width: 100%;
  max-width: 320px;
  max-height: 240px;
  object-fit: contain;
  border-radius: 8px;
}

.foto-acciones {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin: 8px 0;
}

ion-thumbnail img {
  object-fit: cover;
  border-radius: 6px;
}
</style>
