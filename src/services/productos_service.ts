import { ajax_binario, ajax_request } from './ajax_service';
import { obtener_api_url } from '../config/debug';
import { validar_foto } from './camara_service';

export type Producto = {
  id: number;
  empresa_id: number;
  nombre_empresa: string;
  nombre: string;
  descripcion: string | null;
  precio: number;
  stock: boolean;
  fotoUrl: string | null;
};

export type ProductoRequest = {
  empresa_id?: number | null;
  nombre: string;
  descripcion: string | null;
  precio: number;
  stock: boolean;
  foto?: File | null;
};

export type ProductosResponse = {
  productos: Producto[];
  pagina: number;
  tamanio: number;
  total: number;
  total_paginas: number;
};

type ProductoApi = Omit<Producto, 'stock' | 'fotoUrl'> & {
  stock: boolean | number | string;
  fotoUrl?: string | null;
};

type ProductosApiResponse = Omit<ProductosResponse, 'productos'> & {
  productos: ProductoApi[];
};

function normalizar_stock(stock: boolean | number | string) {
  if (typeof stock === 'number') {
    return stock !== 0;
  }

  return stock === true || stock === '1' || stock === 'true';
}

function normalizar_producto(producto: ProductoApi): Producto {
  return {
    ...producto,
    stock: normalizar_stock(producto.stock),
    fotoUrl: producto.fotoUrl || null,
  };
}

export function resolver_foto_url(fotoUrl: string | null) {
  if (!fotoUrl) return '';
  try {
    const url = new URL(fotoUrl, `${obtener_api_url()}/`);
    return ['http:', 'https:'].includes(url.protocol) ? url.href : '';
  } catch {
    return '';
  }
}

export function obtener_foto_producto(fotoUrl: string) {
  if (!/^\/uploads\/productos\/[a-zA-Z0-9-]+\.(jpg|png|webp)$/.test(fotoUrl)) throw new Error('Ruta de foto invalida.');
  return ajax_binario(fotoUrl);
}

function crear_form_data(producto: ProductoRequest) {
  const datos = new FormData();
  datos.append('nombre', producto.nombre);
  datos.append('descripcion', producto.descripcion || '');
  datos.append('precio', String(producto.precio));
  datos.append('stock', String(producto.stock));
  if (producto.empresa_id) datos.append('empresaId', String(producto.empresa_id));
  if (producto.foto) {
    const mensaje = validar_foto(producto.foto);
    if (mensaje) throw new Error(mensaje);
    datos.append('foto', producto.foto, producto.foto.name);
  }
  return datos;
}

export async function obtener_productos(pagina = 1, tamanio = 10, empresa_id?: number | null) {
  const respuesta = await ajax_request<ProductosApiResponse>(
    `/api/productos?pagina=${pagina}&tamanio=${tamanio}${empresa_id ? `&empresa_id=${empresa_id}` : ''}`,
  );

  return {
    ...respuesta,
    productos: respuesta.productos.map(normalizar_producto),
  };
}

export async function obtener_producto(id: number) {
  const producto = await ajax_request<ProductoApi>(`/api/productos/${id}`);

  return normalizar_producto(producto);
}

export function crear_producto(producto: ProductoRequest) {
  return ajax_request<Producto>('/api/productos', {
    method: 'POST',
    body: crear_form_data(producto),
  });
}

export function actualizar_producto(id: number, producto: ProductoRequest) {
  return ajax_request<Producto>(`/api/productos/${id}`, {
    method: 'PUT',
    body: crear_form_data(producto),
  });
}

export function eliminar_producto(id: number) {
  return ajax_request<void>(`/api/productos/${id}`, {
    method: 'DELETE',
  });
}
