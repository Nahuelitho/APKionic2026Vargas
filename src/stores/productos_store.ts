import { computed, ref, watch } from 'vue';
import { sesion } from '../services/auth_service';
import {
  actualizar_producto as actualizar_producto_api,
  crear_producto as crear_producto_api,
  eliminar_producto as eliminar_producto_api,
  obtener_productos,
  type Producto,
  type ProductoRequest,
} from '../services/productos_service';

const productos = ref<Producto[]>([]);
const cargando = ref(false);
const guardando = ref(false);
const eliminando = ref(false);
const error = ref<string | null>(null);
const error_formulario = ref<string | null>(null);

const pagina = ref(1);
const tamanio = ref(5);
const total = ref(0);
const total_paginas = ref(0);
let version = 0;
watch(() => `${sesion.usuario.value?.id}:${sesion.usuario.value?.rol}:${sesion.usuario.value?.empresa_id}`, () => {
  ++version; productos.value = []; total.value = 0; total_paginas.value = 0; pagina.value = 1;
  cargando.value = false; error.value = null; error_formulario.value = null;
}, { flush: 'sync' });

const hay_productos = computed(() => productos.value.length > 0);
const hay_mas_productos = computed(() => pagina.value < total_paginas.value);
const puede_ir_anterior = computed(() => pagina.value > 1);
const puede_ir_siguiente = computed(() => pagina.value < total_paginas.value);

async function cargar_productos() {
  const actual = ++version;
  cargando.value = true;
  error.value = null;
  pagina.value = 1;

  try {
    const respuesta = await obtener_productos(pagina.value, tamanio.value);
    if (actual !== version) return;

    productos.value = respuesta.productos;
    pagina.value = respuesta.pagina;
    tamanio.value = respuesta.tamanio;
    total.value = respuesta.total;
    total_paginas.value = respuesta.total_paginas;
  } catch (excepcion) {
    if (actual !== version) return;
    const ajax_error = excepcion as { mensaje?: string };

    productos.value = [];
    error.value = ajax_error.mensaje || 'No se pudieron cargar los productos.';
  } finally {
    if (actual === version) cargando.value = false;
  }
}

async function cargar_mas_productos() {
  if (cargando.value || !hay_mas_productos.value) {
    return;
  }

  cargando.value = true;
  const actual = version;
  error.value = null;

  try {
    const respuesta = await obtener_productos(pagina.value + 1, tamanio.value);
    if (actual !== version) return;
    const ids_conocidos = productos.value.map((producto) => producto.id);

    productos.value = [
      ...productos.value,
      ...respuesta.productos.filter((producto) => !ids_conocidos.includes(producto.id)),
    ];
    pagina.value = respuesta.pagina;
    tamanio.value = respuesta.tamanio;
    total.value = respuesta.total;
    total_paginas.value = respuesta.total_paginas;
  } catch (excepcion) {
    if (actual !== version) return;
    const ajax_error = excepcion as { mensaje?: string };

    error.value = ajax_error.mensaje || 'No se pudieron cargar mas productos.';
  } finally {
    if (actual === version) cargando.value = false;
  }
}

async function crear_producto(producto: ProductoRequest) {
  guardando.value = true;
  error_formulario.value = null;

  try {
    await crear_producto_api(producto);
    await cargar_productos();
    return true;
  } catch (excepcion) {
    const ajax_error = excepcion as { mensaje?: string; message?: string };

    error_formulario.value = ajax_error.mensaje || ajax_error.message || 'No se pudo crear el producto.';
    return false;
  } finally {
    guardando.value = false;
  }
}

async function actualizar_producto(id: number, producto: ProductoRequest) {
  guardando.value = true;
  error_formulario.value = null;

  try {
    await actualizar_producto_api(id, producto);
    await cargar_productos();
    return true;
  } catch (excepcion) {
    const ajax_error = excepcion as { mensaje?: string; message?: string };

    error_formulario.value = ajax_error.mensaje || ajax_error.message || 'No se pudo actualizar el producto.';
    return false;
  } finally {
    guardando.value = false;
  }
}

async function eliminar_producto(id: number) {
  eliminando.value = true;
  error.value = null;

  try {
    await eliminar_producto_api(id);
    await cargar_productos();
    return true;
  } catch (excepcion) {
    const ajax_error = excepcion as { mensaje?: string };

    error.value = ajax_error.mensaje || 'No se pudo eliminar el producto.';
    return false;
  } finally {
    eliminando.value = false;
  }
}

async function ir_pagina_anterior() {
  if (!puede_ir_anterior.value) {
    return;
  }

  pagina.value -= 1;
  await cargar_productos();
}

async function ir_pagina_siguiente() {
  await cargar_mas_productos();
}

export function use_productos_store() {
  return {
    productos,
    cargando,
    guardando,
    eliminando,
    error,
    error_formulario,
    pagina,
    tamanio,
    total,
    total_paginas,
    hay_productos,
    hay_mas_productos,
    puede_ir_anterior,
    puede_ir_siguiente,
    cargar_productos,
    cargar_mas_productos,
    crear_producto,
    actualizar_producto,
    eliminar_producto,
    ir_pagina_anterior,
    ir_pagina_siguiente,
  };
}
