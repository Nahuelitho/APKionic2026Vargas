import { ajax_request } from './ajax_service';

export type Empresa = {
  id: number;
  nombre_empresa: string;
  activo: boolean;
  direccion_linea?: string | null;
  referencia?: string | null;
  latitud?: number | null;
  longitud?: number | null;
};

export const obtener_empresas = () => ajax_request<Empresa[]>('/api/empresas', { guardable: true });
export const crear_empresa = (nombre_empresa: string) => ajax_request<Empresa>('/api/empresas', { method: 'POST', body: { nombre_empresa } });
export const actualizar_empresa = (empresa: Empresa) => ajax_request<Empresa>(`/api/empresas/${empresa.id}`, { method: 'PUT', body: empresa });
export const guardar_ubicacion_local = (
  id: number,
  body: {
    direccion_linea?: string | null;
    referencia?: string | null;
    latitud?: number | null;
    longitud?: number | null;
  }
) => ajax_request<Empresa>(`/api/empresas/${id}/ubicacion`, { method: 'PUT', body });