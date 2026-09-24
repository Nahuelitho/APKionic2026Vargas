import { ajax_request } from './ajax_service';

export type UsuarioAdmin = { id: number; nombre: string; email: string; activo: boolean; rol_id: number | null; rol: string | null };
export type Rol = { id: number; nombre: string; codigo: string };

export const listar_usuarios = () => ajax_request<UsuarioAdmin[]>('/api/usuarios');
export const listar_roles = () => ajax_request<Rol[]>('/api/usuarios/roles');
export const crear_usuario = (datos: { nombre: string; email: string; password: string; rol_id: number | null }) =>
  ajax_request<{ id: number }>('/api/usuarios', { method: 'POST', body: datos });
export const asignar_rol = (id: number, rol_id: number | null) =>
  ajax_request<void>(`/api/usuarios/${id}/rol`, { method: 'PUT', body: { rol_id } });
