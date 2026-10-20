<template>
  <ion-card>
    <ion-card-header><ion-card-title>{{ empresa ? 'Catalogo y carrito' : 'Nuevo pedido' }}</ion-card-title></ion-card-header>
    <ion-card-content>
      <ion-input v-model="cliente" label="Cliente" label-placement="stacked" :maxlength="160" :disabled="creando" />
      <ion-select v-if="!empresa" :model-value="empresa_id" label="Empresa del pedido" label-placement="stacked" :disabled="creando || cargando_empresas" @ionChange="cambiar_empresa($event.detail.value, $event)">
        <ion-select-option v-for="opcion in empresas" :key="opcion.id" :value="opcion.id">{{ opcion.nombre_empresa }}</ion-select-option>
      </ion-select>
      <p v-if="cargando_empresas">Cargando empresas...</p>
      <ion-text v-if="error_empresas" color="danger"><p role="alert">{{ error_empresas }}</p></ion-text>
      <ion-button v-if="error_empresas" fill="outline" @click="cargar_empresas">Reintentar empresas</ion-button>
      <p>Selecciona productos disponibles del catalogo.</p>
      <p v-if="cargando_productos">Cargando productos...</p>
      <ion-text v-if="error_productos" color="danger"><p role="alert">{{ error_productos }}</p></ion-text>
      <ion-button v-if="error_productos" fill="outline" :disabled="creando" @click="cargar_productos(pagina)">Reintentar catalogo</ion-button>
      <ion-list>
        <ion-item v-for="producto in productos" :key="producto.id">
          <comp-producto-foto v-if="producto.fotoUrl" slot="start" class="foto" :foto-url="producto.fotoUrl" :alt="producto.nombre" />
          <ion-label class="ion-text-wrap">{{ producto.nombre }}<p v-if="producto.descripcion">{{ producto.descripcion }}</p><p>{{ dinero(producto.precio) }} · {{ producto.stock ? 'Disponible' : 'Sin stock' }}</p></ion-label>
          <ion-button slot="end" fill="outline" :disabled="creando || !producto.stock || renglones.length >= 100 || renglones.some(r => r.producto.id === producto.id)" @click="agregar(producto)">Agregar</ion-button>
        </ion-item>
      </ion-list>
      <p v-if="!cargando_productos && !error_productos && !productos.length">No hay productos.</p>
      <div class="acciones">
        <ion-button fill="clear" :disabled="cargando_productos || pagina <= 1 || creando" @click="cargar_productos(pagina - 1)">Anterior</ion-button>
        <span>Pagina {{ pagina }} de {{ Math.max(1, total_paginas) }}</span>
        <ion-button fill="clear" :disabled="cargando_productos || pagina >= total_paginas || creando" @click="cargar_productos(pagina + 1)">Siguiente</ion-button>
      </div>
      <h2>Carrito</h2>
      <p v-if="!renglones.length">Agrega productos para preparar tu pedido.</p>
      <div v-for="renglon in renglones" :key="renglon.producto.id" class="renglon">
        <div><strong>{{ renglon.producto.nombre }}</strong><p>{{ dinero(renglon.producto.precio) }} por unidad</p></div>
        <ion-input v-model.number="renglon.cantidad" type="number" label="Cantidad" label-placement="stacked" min="1" max="10000" step="1" :disabled="creando" />
        <span>{{ dinero(renglon.producto.precio * Number(renglon.cantidad)) }}</span>
        <ion-button fill="clear" color="danger" :disabled="creando" @click="renglones = renglones.filter(r => r !== renglon)">Quitar</ion-button>
      </div>
      <p><strong>Total orientativo: {{ dinero(total_orientativo) }}</strong></p>
      <p>El servidor confirma los precios y la disponibilidad al crear el pedido.</p>
      <ion-text v-if="error_crear" color="danger"><p role="alert">{{ error_crear }}</p></ion-text>
      <ion-button expand="block" :disabled="creando || !renglones.length || !empresa_id" @click="crear">{{ creando ? 'Creando...' : 'Confirmar pedido' }}</ion-button>
    </ion-card-content>
  </ion-card>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { onBeforeRouteLeave, onBeforeRouteUpdate } from 'vue-router';
import { IonButton, IonCard, IonCardContent, IonCardHeader, IonCardTitle, IonInput, IonItem, IonLabel, IonList, IonSelect, IonSelectOption, IonText } from '@ionic/vue';
import CompProductoFoto from './comp-producto-foto.vue';
import { sesion } from '../services/auth_service';
import { obtener_empresas, type Empresa } from '../services/empresas_service';
import { obtener_productos, type Producto } from '../services/productos_service';
import { crear_pedido } from '../services/pedidos_service';

const props = defineProps<{ empresa?: Empresa }>();
const emit = defineEmits<{ creado: [id: number] }>();
const empresas = ref<Empresa[]>([]), empresa_id = ref<number | null>(null);
const productos = ref<Producto[]>([]), pagina = ref(1), total_paginas = ref(0);
const cliente = ref(''), renglones = ref<{ producto: Producto; cantidad: number | string }[]>([]);
const cargando_empresas = ref(false), cargando_productos = ref(false), creando = ref(false);
const error_empresas = ref(''), error_productos = ref(''), error_crear = ref('');
let empresas_version = 0, productos_version = 0;
let activo = true;
const total_orientativo = computed(() => renglones.value.reduce((total, r) => total + r.producto.precio * Number(r.cantidad), 0));
const dinero = (valor: number) => Number.isFinite(valor) ? new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(valor) : 'Cantidad invalida';
function mensaje(error: unknown) {
  if (error && typeof error === 'object' && 'mensaje' in error) return String(error.mensaje);
  return error instanceof Error ? error.message : 'No se pudo completar la operacion.';
}
function permitir_salida() {
  if (creando.value) return false;
  return !renglones.value.length || window.confirm('Salir y descartar el carrito sin confirmar?');
}
onBeforeRouteLeave(permitir_salida);
onBeforeRouteUpdate((destino, origen) => destino.params.empresaId === origen.params.empresaId || permitir_salida());
function antes_de_cerrar(event: BeforeUnloadEvent) {
  if (!creando.value && !renglones.value.length) return;
  event.preventDefault(); event.returnValue = '';
}
window.addEventListener('beforeunload', antes_de_cerrar);
async function cargar_productos(numero = 1) {
  if (!activo || creando.value) return;
  const version = ++productos_version, id = empresa_id.value;
  productos.value = []; cargando_productos.value = true; error_productos.value = '';
  try {
    if (!id) return;
    const respuesta = await obtener_productos(numero, 10, id);
    if (!activo || version !== productos_version || id !== empresa_id.value) return;
    productos.value = respuesta.productos.filter(p => p.empresa_id === id);
    pagina.value = respuesta.pagina; total_paginas.value = respuesta.total_paginas;
  } catch (error) { if (activo && version === productos_version) error_productos.value = mensaje(error); }
  finally { if (activo && version === productos_version) cargando_productos.value = false; }
}
function cambiar_empresa(id: number, event?: CustomEvent) {
  if (id === empresa_id.value) return;
  if (creando.value || !empresas.value.some(e => e.id === id) || (renglones.value.length && !window.confirm('Cambiar de empresa y descartar el carrito?'))) {
    // Restore Ionic's internal selection without changing the cart's company.
    if (event?.target) (event.target as HTMLIonSelectElement).value = empresa_id.value;
    return;
  }
  empresa_id.value = id; renglones.value = []; error_crear.value = '';
  pagina.value = 1; total_paginas.value = 0;
  void cargar_productos();
}
async function cargar_empresas() {
  const version = ++empresas_version;
  cargando_empresas.value = true; error_empresas.value = '';
  try {
    const respuesta = await obtener_empresas();
    if (!activo || version !== empresas_version) return;
    empresas.value = respuesta.filter(e => e.activo);
    const primera = empresas.value[0];
    if (primera) cambiar_empresa(primera.id);
    else error_empresas.value = 'No hay empresas activas disponibles.';
  } catch (error) { if (activo && version === empresas_version) error_empresas.value = mensaje(error); }
  finally { if (activo && version === empresas_version) cargando_empresas.value = false; }
}
function agregar(producto: Producto) {
  if (!creando.value && producto.empresa_id === empresa_id.value && producto.stock && renglones.value.length < 100 && !renglones.value.some(r => r.producto.id === producto.id)) renglones.value.push({ producto, cantidad: 1 });
}
async function crear() {
  const usuario = sesion.usuario.value;
  if (!activo || creando.value || !usuario || !['superadmin', 'administrador', 'vendedor', ''].includes(usuario.rol || '')) return;
  error_crear.value = '';
  const nombre = cliente.value.trim();
  if (!nombre || nombre.length > 160) { error_crear.value = 'El cliente es obligatorio y admite hasta 160 caracteres.'; return; }
  if (!renglones.value.length || renglones.value.length > 100 || renglones.value.some(r => !Number.isInteger(Number(r.cantidad)) || Number(r.cantidad) < 1 || Number(r.cantidad) > 10000)) {
    error_crear.value = 'Agrega entre 1 y 100 productos con cantidades enteras de 1 a 10000.'; return;
  }
  if (!Number.isFinite(total_orientativo.value) || total_orientativo.value > 9999999999.99) { error_crear.value = 'El total supera el importe permitido.'; return; }
  if (!empresa_id.value || renglones.value.some(r => r.producto.empresa_id !== empresa_id.value)) { error_crear.value = 'Seleccione productos de una unica empresa.'; return; }
  creando.value = true;
  try {
    const nuevo = await crear_pedido({ cliente: nombre, empresa_id: empresa_id.value, items: renglones.value.map(r => ({ productoId: r.producto.id, cantidad: Number(r.cantidad) })) });
    if (!activo) return;
    cliente.value = ''; renglones.value = []; creando.value = false;
    emit('creado', nuevo.id);
  } catch (error) { if (activo) error_crear.value = mensaje(error); }
  finally { if (activo) creando.value = false; }
}
watch(() => props.empresa?.id, () => {
  ++productos_version; renglones.value = []; productos.value = []; cliente.value = ''; error_crear.value = '';
  empresa_id.value = props.empresa?.id ?? null; pagina.value = 1; total_paginas.value = 0;
  if (props.empresa) void cargar_productos();
  else void cargar_empresas();
}, { immediate: true });
onBeforeUnmount(() => {
  activo = false; ++empresas_version; ++productos_version;
  window.removeEventListener('beforeunload', antes_de_cerrar);
});
</script>

<style scoped>
.acciones { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
.foto { width: 56px; height: 56px; object-fit: cover; border-radius: 8px; }
.renglon { display: grid; grid-template-columns: minmax(0, 1fr) 110px auto auto; align-items: center; gap: 12px; padding: 12px 0; border-bottom: 1px solid var(--ion-color-light-shade); }
.renglon p { margin: 4px 0; }
@media (max-width: 600px) { .renglon { grid-template-columns: minmax(0, 1fr) 100px; } }
</style>
