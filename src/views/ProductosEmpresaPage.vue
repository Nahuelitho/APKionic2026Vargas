<template>
  <comp-page :titulo="empresa ? `Productos · ${empresa.nombre_empresa}` : 'Productos de la empresa'" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-button fill="outline" @click="volver">Volver</ion-button>

    <comp-esqueleto v-if="cargando && !empresa" />

    <ion-card v-if="error && !empresa">
      <ion-card-header><ion-card-title>No se pudo abrir la empresa</ion-card-title></ion-card-header>
      <ion-card-content>
        <p>{{ error }}</p>
        <ion-button expand="block" @click="cargar">Reintentar</ion-button>
      </ion-card-content>
    </ion-card>

    <template v-if="empresa">
      <ion-card v-if="puede_gestionar">
        <ion-card-header>
          <ion-card-title>{{ producto_editando_id ? 'Modificar producto' : 'Nuevo producto' }}</ion-card-title>
          <ion-card-subtitle>{{ empresa.nombre_empresa }}</ion-card-subtitle>
        </ion-card-header>

        <ion-card-content>
          <ion-input v-model="formulario.nombre" label="Nombre" label-placement="stacked" placeholder="Ej: Yerba mate" />
          <ion-textarea v-model="formulario.descripcion" label="Descripcion" label-placement="stacked" placeholder="Detalle del producto" />
          <ion-input v-model.number="formulario.precio" type="number" label="Precio" label-placement="stacked" placeholder="0" />

          <ion-item lines="none">
            <ion-toggle :key="`${producto_editando_id || 'nuevo'}-${formulario.stock}`" :checked="formulario.stock" @ionChange="cambiar_stock">
              {{ formulario.stock ? 'Con stock' : 'Sin stock' }}
            </ion-toggle>
          </ion-item>

          <div class="foto-formulario">
            <p>Foto del producto (JPEG, PNG o WebP, hasta 5 MB)</p>
            <img v-if="foto_preview" :src="foto_preview" alt="Vista previa de la foto del producto" class="foto-preview" />
            <comp-producto-foto v-else-if="foto_actual_url" :foto-url="foto_actual_url" alt="Foto actual del producto" class="foto-preview" />
            <div class="foto-acciones">
              <template v-if="es_nativo">
                <ion-button fill="outline" :disabled="guardando || seleccionando_foto || cargando_edicion" @click="seleccionar_foto('camara')">Camara</ion-button>
                <ion-button fill="outline" :disabled="guardando || seleccionando_foto || cargando_edicion" @click="seleccionar_foto('galeria')">Galeria</ion-button>
              </template>
              <ion-button v-else fill="outline" :disabled="guardando || cargando_edicion" @click="selector_foto?.click()">Seleccionar foto</ion-button>
              <ion-button v-if="foto_archivo" fill="clear" color="medium" :disabled="guardando" @click="descartar_foto">Descartar foto nueva</ion-button>
            </div>
            <input ref="selector_foto" type="file" accept="image/jpeg,image/png,image/webp" hidden @change="seleccionar_archivo" />
            <p v-if="seleccionando_foto">Obteniendo foto...</p>
            <ion-text v-if="error_foto" color="danger"><p>{{ error_foto }}</p></ion-text>
          </div>

          <ion-text v-if="error_formulario" color="danger"><p>{{ error_formulario }}</p></ion-text>

          <ion-button expand="block" :disabled="guardando || seleccionando_foto || cargando_edicion" @click="guardar">
            {{ guardando ? 'Guardando...' : producto_editando_id ? 'Guardar cambios' : 'Crear producto' }}
          </ion-button>
          <ion-button v-if="producto_editando_id || foto_archivo || cargando_edicion" expand="block" fill="clear" color="medium" :disabled="guardando" @click="cancelar_edicion">
            {{ producto_editando_id || cargando_edicion ? 'Cancelar edicion' : 'Limpiar formulario' }}
          </ion-button>
        </ion-card-content>
      </ion-card>

      <ion-card>
        <ion-card-header>
          <ion-card-title>Productos de {{ empresa.nombre_empresa }}</ion-card-title>
          <ion-card-subtitle>{{ total }} producto{{ total === 1 ? '' : 's' }}</ion-card-subtitle>
        </ion-card-header>
        <ion-card-content>
          <comp-esqueleto v-if="cargando && !productos.length" />

          <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
          <ion-button v-if="error" fill="outline" @click="cargar_productos(pagina)">Reintentar</ion-button>

          <p v-else-if="!cargando && !productos.length">Esta empresa todavía no tiene productos cargados.</p>

          <ion-list v-else>
            <ion-item v-for="producto in productos" :key="producto.id">
              <ion-thumbnail v-if="producto.fotoUrl" slot="start">
                <comp-producto-foto :foto-url="producto.fotoUrl" :alt="`Foto de ${producto.nombre}`" />
              </ion-thumbnail>
              <ion-label class="ion-text-wrap">
                <h3>{{ producto.nombre }}</h3>
                <p>{{ producto.descripcion || 'Sin descripcion' }}</p>
                <p>{{ dinero(producto.precio) }}</p>
              </ion-label>

              <ion-badge v-if="!producto.stock" color="warning">Sin stock</ion-badge>

              <template v-if="puede_gestionar">
                <ion-button fill="clear" size="small" :disabled="guardando || seleccionando_foto || cargando_edicion" @click="editar(producto)">Editar</ion-button>
                <ion-button fill="clear" size="small" color="danger" :disabled="eliminando" @click="borrar(producto.id)">Borrar</ion-button>
              </template>
            </ion-item>
          </ion-list>

          <div v-if="total_paginas > 1" class="paginacion">
            <ion-button fill="clear" :disabled="cargando || pagina <= 1" @click="cargar_productos(pagina - 1)">Anterior</ion-button>
            <span>Pagina {{ pagina }} de {{ total_paginas }}</span>
            <ion-button fill="clear" :disabled="cargando || pagina >= total_paginas" @click="cargar_productos(pagina + 1)">Siguiente</ion-button>
          </div>
        </ion-card-content>
      </ion-card>
    </template>
  </comp-page>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Capacitor } from '@capacitor/core';
import {
  IonBadge, IonButton, IonCard, IonCardContent, IonCardHeader, IonCardSubtitle, IonCardTitle,
  IonInput, IonItem, IonLabel, IonList, IonText, IonTextarea, IonThumbnail, IonToggle,
  onIonViewDidLeave, onIonViewWillEnter,
} from '@ionic/vue';

import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import CompProductoFoto from '../components/comp-producto-foto.vue';
import { obtener_producto, obtener_productos, eliminar_producto as eliminar_producto_api, crear_producto, actualizar_producto, type Producto } from '../services/productos_service';
import { obtener_foto, validar_foto } from '../services/camara_service';
import { sesion } from '../services/auth_service';
import { obtener_empresas, type Empresa } from '../services/empresas_service';

const route = useRoute();
const router = useRouter();

const empresa = ref<Empresa | null>(null);
const productos = ref<Producto[]>([]);
const cargando = ref(false), guardando = ref(false), eliminando = ref(false);
const error = ref(''), error_formulario = ref<string | null>(null), error_foto = ref<string | null>(null);
const pagina = ref(1), tamanio = ref(10), total = ref(0), total_paginas = ref(0);

const es_nativo = Capacitor.isNativePlatform();
const selector_foto = ref<HTMLInputElement | null>(null);
const foto_archivo = ref<File | null>(null);
const foto_preview = ref('');
const foto_actual_url = ref('');
const seleccionando_foto = ref(false);
const cargando_edicion = ref(false);
const producto_editando_id = ref<number | null>(null);

let empresa_version = 0;
let productos_version = 0;
let version_formulario = 0;

const formulario = reactive({ nombre: '', descripcion: '', precio: 0, stock: true });

const es_superadmin = computed(() => sesion.usuario.value?.rol === 'superadmin');
const puede_gestionar = computed(() => Boolean(empresa.value) && (
  es_superadmin.value ||
  (['administrador', 'vendedor'].includes(sesion.usuario.value?.rol || '') &&
    sesion.usuario.value?.empresa_id === empresa.value?.id)
));

const dinero = (valor: number) => Number.isFinite(valor)
  ? new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(valor)
  : 'Importe invalido';

function mensaje(excepcion: unknown) {
  if (excepcion && typeof excepcion === 'object' && 'mensaje' in excepcion) return String((excepcion as { mensaje: unknown }).mensaje);
  return excepcion instanceof Error ? excepcion.message : 'No se pudo completar la operacion.';
}

function id_empresa(): number | null {
  const parametro = route.params.empresaId;
  const id = typeof parametro === 'string' && /^[1-9]\d*$/.test(parametro) ? Number(parametro) : NaN;
  return Number.isSafeInteger(id) ? id : null;
}

async function cargar_empresa() {
  const actual = ++empresa_version;
  empresa.value = null;
  error.value = '';
  const id = id_empresa();
  if (id === null) { error.value = 'Empresa invalida.'; return false; }

  try {
    const lista = await obtener_empresas();
    if (actual !== empresa_version) return false;
    const encontrada = lista.find(item => item.id === id) ?? null;

    // El backend ya filtra por ambito; esta validacion evita mostrar una empresa ajena.
    if (!encontrada || (!es_superadmin.value && sesion.usuario.value?.empresa_id !== id)) {
      error.value = 'La empresa no existe o no tenes acceso a sus productos.';
      return false;
    }

    empresa.value = encontrada;
    return true;
  } catch (excepcion) {
    if (actual === empresa_version) error.value = mensaje(excepcion);
    return false;
  }
}

async function cargar_productos(numero = 1) {
  if (!empresa.value) return;
  const actual = ++productos_version;
  cargando.value = true;
  error.value = '';

  try {
    const respuesta = await obtener_productos(numero, tamanio.value, empresa.value.id);
    if (actual !== productos_version) return;
    productos.value = respuesta.productos;
    pagina.value = respuesta.pagina;
    tamanio.value = respuesta.tamanio;
    total.value = respuesta.total;
    total_paginas.value = respuesta.total_paginas;
  } catch (excepcion) {
    if (actual !== productos_version) return;
    productos.value = [];
    error.value = mensaje(excepcion);
  } finally {
    if (actual === productos_version) cargando.value = false;
  }
}

async function cargar() {
  cargando.value = true;
  limpiar_formulario();
  const ok = await cargar_empresa();
  if (ok) await cargar_productos(1);
  cargando.value = false;
}

async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}

function volver() {
  void router.push('/app/productos');
}

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
  const mensajeValidacion = validar_foto(archivo);
  if (mensajeValidacion) { error_foto.value = mensajeValidacion; return; }
  descartar_foto();
  foto_archivo.value = archivo;
  foto_preview.value = URL.createObjectURL(archivo);
}

function seleccionar_archivo(event: Event) {
  const input = event.target as HTMLInputElement;
  const archivo = input.files?.[0];
  if (archivo && puede_gestionar.value && !guardando.value && !cargando_edicion.value) usar_archivo(archivo);
  input.value = '';
}

async function seleccionar_foto(origen: 'camara' | 'galeria') {
  if (!puede_gestionar.value || guardando.value || seleccionando_foto.value || cargando_edicion.value) return;
  const version = version_formulario;
  seleccionando_foto.value = true;
  error_foto.value = null;
  const resultado = await obtener_foto(origen);
  if (version !== version_formulario) return;
  seleccionando_foto.value = false;
  if (resultado.ok && resultado.archivo) usar_archivo(resultado.archivo);
  else error_foto.value = resultado.mensaje;
}

async function guardar() {
  if (!puede_gestionar.value || !empresa.value || guardando.value || seleccionando_foto.value || cargando_edicion.value) return;
  if (!formulario.nombre.trim()) { error_formulario.value = 'El nombre del producto es obligatorio.'; return; }
  if (!Number.isFinite(Number(formulario.precio)) || Number(formulario.precio) <= 0) { error_formulario.value = 'El precio debe ser mayor a 0.'; return; }
  if (foto_archivo.value) {
    error_foto.value = validar_foto(foto_archivo.value);
    if (error_foto.value) return;
  }

  const version = version_formulario;
  const producto = {
    nombre: formulario.nombre.trim(),
    descripcion: formulario.descripcion.trim() || null,
    precio: Number(formulario.precio),
    stock: formulario.stock,
    foto: foto_archivo.value,
    empresa_id: empresa.value.id,
  };

  guardando.value = true;
  error_formulario.value = null;
  try {
    if (producto_editando_id.value) await actualizar_producto(producto_editando_id.value, producto);
    else await crear_producto(producto);
    if (version !== version_formulario) return;
    limpiar_formulario();
    await cargar_productos(1);
  } catch (excepcion) {
    if (version === version_formulario) error_formulario.value = mensaje(excepcion);
  } finally {
    if (version === version_formulario) guardando.value = false;
  }
}

async function editar(producto: Producto) {
  if (!puede_gestionar.value || guardando.value || seleccionando_foto.value || cargando_edicion.value) return;
  limpiar_formulario();
  const version = version_formulario;
  cargando_edicion.value = true;
  try {
    const producto_api = await obtener_producto(producto.id);
    if (version !== version_formulario) return;
    producto_editando_id.value = producto_api.id;
    formulario.nombre = producto_api.nombre;
    formulario.descripcion = producto_api.descripcion || '';
    formulario.precio = producto_api.precio;
    formulario.stock = producto_api.stock;
    foto_actual_url.value = producto_api.fotoUrl || '';
  } catch (excepcion) {
    if (version === version_formulario) error_formulario.value = mensaje(excepcion);
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
  if (!puede_gestionar.value || eliminando.value) return;
  if (!window.confirm('Seguro que queres eliminar este producto?')) return;

  eliminando.value = true;
  error.value = '';
  try {
    await eliminar_producto_api(id);
    if (producto_editando_id.value === id) limpiar_formulario();
    await cargar_productos(pagina.value);
  } catch (excepcion) {
    error.value = mensaje(excepcion);
  } finally {
    eliminando.value = false;
  }
}

watch(() => route.params.empresaId, () => { void cargar(); });
onIonViewWillEnter(() => { void cargar(); });
onIonViewDidLeave(limpiar_formulario);
onBeforeUnmount(() => { ++empresa_version; ++productos_version; limpiar_formulario(); });
</script>

<style scoped>
.foto-formulario { margin: 12px 0; }
.foto-preview { display: block; width: min(100%, 240px); height: auto; margin: 8px 0; border-radius: 8px; object-fit: cover; }
.foto-acciones { display: flex; flex-wrap: wrap; gap: 8px; }
.paginacion { display: flex; align-items: center; justify-content: center; flex-wrap: wrap; gap: 8px; padding-top: 8px; }
</style>
