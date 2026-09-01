import { computed, ref } from 'vue';
import { obtener_productos, type Producto } from '../services/productos_service';

const productos = ref<Producto[]>([]);
const cargando = ref(false);
const error = ref<string | null>(null);

const hay_productos = computed(() => productos.value.length > 0);

async function cargar_productos() {
  cargando.value = true;
  error.value = null;

  try {
    const respuesta = await obtener_productos();
    productos.value = respuesta.productos;
  } catch (excepcion) {
    const ajax_error = excepcion as { mensaje?: string };

    productos.value = [];
    error.value = ajax_error.mensaje || 'No se pudieron cargar los productos.';
  } finally {
    cargando.value = false;
  }
}

export function use_productos_store() {
  return {
    productos,
    cargando,
    error,
    hay_productos,
    cargar_productos,
  };
}