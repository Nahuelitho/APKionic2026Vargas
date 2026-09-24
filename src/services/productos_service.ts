import { ajax_request } from './ajax_service';

export type Producto = {
  id: number;
  nombre: string;
  descripcion: string | null;
  precio: number;
  stock: boolean;
};

export type ProductoRequest = {
  nombre: string;
  descripcion: string | null;
  precio: number;
  stock: boolean;
};

export type ProductosResponse = {
  productos: Producto[];
  pagina: number;
  tamanio: number;
  total: number;
  total_paginas: number;
};

type ProductoApi = Omit<Producto, 'stock'> & {
  stock: boolean | number | string;
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
  };
}

export async function obtener_productos(pagina = 1, tamanio = 10) {
  const respuesta = await ajax_request<ProductosApiResponse>(
    `/api/productos?pagina=${pagina}&tamanio=${tamanio}`,
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
    body: producto,
  });
}

export function actualizar_producto(id: number, producto: ProductoRequest) {
  return ajax_request<Producto>(`/api/productos/${id}`, {
    method: 'PUT',
    body: producto,
  });
}

export function eliminar_producto(id: number) {
  return ajax_request<void>(`/api/productos/${id}`, {
    method: 'DELETE',
  });
}
