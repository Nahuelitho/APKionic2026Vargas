import { ajax_request } from './ajax_service';

export type Producto = {
  id: number;
  nombre: string;
  descripcion: string | null;
  precio: number;
  stock: boolean;
};

export type ProductosResponse = {
  productos: Producto[];
};

export function obtener_productos() {
  return ajax_request<ProductosResponse>('/api/productos');
}