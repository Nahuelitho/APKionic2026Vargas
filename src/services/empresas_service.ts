import { ajax_request } from './ajax_service';

export type Empresa = { id: number; nombre_empresa: string; activo: boolean };
export const obtener_empresas = () => ajax_request<Empresa[]>('/api/empresas');
export const crear_empresa = (nombre_empresa: string) => ajax_request<Empresa>('/api/empresas', { method: 'POST', body: { nombre_empresa } });
export const actualizar_empresa = (empresa: Empresa) => ajax_request<void>(`/api/empresas/${empresa.id}`, { method: 'PUT', body: empresa });
