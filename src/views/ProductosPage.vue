<script setup lang="ts">
import { onBeforeUnmount, reactive, ref, watch, computed } from 'vue';
import { useRoute } from 'vue-router';
import { Capacitor } from '@capacitor/core';

import {
  IonBadge,
  IonButton,
  IonCard,
  IonCardContent,
  IonCardHeader,
  IonCardSubtitle,
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

const route = useRoute();

const {
  productos,
  cargando,
  guardando,
  eliminando,
  error,
  error_formulario,
  total,
  hay_mas_productos,
  cargar_productos,
  cargar_mas_productos,
  crear_producto,
  actualizar_producto,
  eliminar_producto,
} = use_productos_store();

const puede_gestionar_productos = computed(() => ['superadmin', 'administrador', 'vendedor'].includes(sesion.usuario.value?.rol || ''));
const es_superadmin = computed(() => sesion.usuario.value?.rol === 'superadmin');

const empresas = ref<Empresa[]>([]);
const empresa_id = ref<number | null>(null);
const filtro_empresa_id = ref<number | null>(null);

const empresas_activas = computed(() => empresas.value.filter(e => e.activo));

const productos_filtrados = computed(() => {
  if (es_superadmin.value && filtro_empresa_id.value != null) {
    return productos.value.filter(p => p.empresa_id === filtro_empresa_id.value);
  }
  if (!es_superadmin.value && empresa_id.value != null) {
    return productos.value.filter(p => p.empresa_id === empresa_id.value);
  }
  return productos.value;
});

const hay_productos_filtrados = computed(() => productos_filtrados.value.length > 0);

const grupos = computed(() => {
  const lista = productos_filtrados.value;
  if (!lista.length) return [];

  const mapa = new Map<number, Producto[]>();
  for (const producto of lista) {
    if (!mapa.has(producto.empresa_id)) mapa.set(producto.empresa_id, []);
    mapa.get(producto.empresa_id)!.push(producto);
  }

  return Array.from(mapa.entries())
    .map(([id, productosGrupo]) => {
      const empresa = empresas.value.find(e => e.id === id);
      const nombre = empresa?.nombre_empresa ?? productosGrupo[0]?.nombre_empresa ?? 'Sin empresa';
      return {
        id,
        nombre,
        productos: productosGrupo,
      };
    })
    .sort((a, b) => a.nombre.localeCompare(b.nombre));
});

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
  if (es_superadmin.value && !producto_editando_id.value && empresa_id.value == null) {
    error_formulario.value = 'Seleccioná la empresa para la que querés crear el producto.';
    return;
  }
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
  try {
    empresas.value = await obtener_empresas();
    const empresaQuery = route.query.empresaId ? Number(route.query.empresaId) : null;
    const empresaQueryValida = empresaQuery && !Number.isNaN(empresaQuery);

    if (es_superadmin.value) {
      filtro_empresa_id.value = empresaQueryValida ? empresaQuery : null;
      empresa_id.value = empresaQueryValida ? empresaQuery : null;
    } else {
      const empresaSesion = sesion.usuario.value?.empresa_id ?? null;
      const empresaElegida = empresaQueryValida ? empresaQuery : empresaSesion;
      empresa_id.value = empresaElegida;
      filtro_empresa_id.value = null;
    }
  }
  catch {
    error_formulario.value = 'No se pudieron cargar las empresas.';
  }
});

watch(puede_gestionar_productos, (puede_gestionar) => {
  if (!puede_gestionar) limpiar_formulario();
});

watch(es_superadmin, () => {
  if (!es_superadmin.value) {
    filtro_empresa_id.value = null;
    empresa_id.value = sesion.usuario.value?.empresa_id ?? null;
  }
});

// Ionic puede conservar la vista montada al navegar.
onIonViewDidLeave(limpiar_formulario);
onBeforeUnmount(limpiar_formulario);
</script>