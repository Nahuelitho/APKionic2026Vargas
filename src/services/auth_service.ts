import { computed, ref } from 'vue';
import { BiometricAuth, BiometryType } from '@aparajita/capacitor-biometric-auth';
import { SecureStorage } from '@aparajita/capacitor-secure-storage';
import { Capacitor } from '@capacitor/core';
import { obtener_api_url } from '../config/debug';

export type UsuarioSesion = { id: number; nombre: string; email: string; rol: string | null };
type SesionResponse = { access_token: string; refresh_token: string; expira_utc: string; usuario: UsuarioSesion };

const CLAVE_TOKEN = 'vargas_token';
const CLAVE_REFRESH = 'vargas_refresh';
const CLAVE_BIOMETRIA = 'auth_biometria';
const usuario = ref<UsuarioSesion | null>(null);
const restaurando = ref(false);
let access_token: string | null = null;
let restauracion: Promise<boolean> | null = null;
let renovacion: Promise<boolean> | null = null;

async function guardar_sesion(sesion: SesionResponse) {
  access_token = sesion.access_token;
  await Promise.all([
    SecureStorage.set(CLAVE_TOKEN, sesion.access_token),
    SecureStorage.set(CLAVE_REFRESH, sesion.refresh_token),
  ]);
  await cargar_usuario();
}

async function solicitar(endpoint: string, body: unknown): Promise<SesionResponse> {
  const url = `${obtener_api_url()}/api/auth/${endpoint}`;
  try {
    const respuesta = await fetch(url, {
      method: 'POST', headers: { Accept: 'application/json', 'Content-Type': 'application/json' }, body: JSON.stringify(body),
    });
    const datos = await respuesta.json().catch(() => null);
    if (!respuesta.ok) {
      console.error('Error de autenticación', { url, estado: respuesta.status, respuesta: datos });
      const mensaje = datos?.mensaje || (respuesta.status >= 500
        ? 'La API tuvo un error interno. Revisá la terminal donde ejecutaste dotnet run.'
        : 'No se pudo iniciar la sesión.');
      throw new Error(`${mensaje} (HTTP ${respuesta.status})`);
    }
    return datos as SesionResponse;
  } catch (error) {
    if (error instanceof TypeError) {
      console.error('No se pudo conectar con la API', { url, error });
      throw new Error(`No se pudo conectar con la API en ${url}. Verificá que dotnet run esté ejecutándose y que la URL sea accesible desde el dispositivo.`);
    }
    throw error;
  }
}

async function cargar_usuario(): Promise<boolean> {
  if (!access_token) return false;
  const respuesta = await fetch(`${obtener_api_url()}/api/sesion/yo`, { headers: { Authorization: `Bearer ${access_token}` } });
  if (!respuesta.ok) return false;
  usuario.value = await respuesta.json() as UsuarioSesion;
  return true;
}

export async function iniciar_sesion(email: string, password: string) {
  await guardar_sesion(await solicitar('login', { email, password }));
}

export async function renovar_sesion(): Promise<boolean> {
  if (renovacion) return renovacion;
  renovacion = (async () => {
    const refresh_token = await SecureStorage.get(CLAVE_REFRESH);
    if (typeof refresh_token !== 'string') return false;
    try { await guardar_sesion(await solicitar('refresh', { refresh_token })); return true; }
    catch { await limpiar_sesion(); return false; }
  })().finally(() => { renovacion = null; });
  return renovacion;
}

export async function restaurar_sesion(): Promise<boolean> {
  if (usuario.value && access_token) return true;
  if (restauracion) return restauracion;
  restaurando.value = true;
  restauracion = (async () => {
    if (localStorage.getItem(CLAVE_BIOMETRIA) === 'true') {
      if (!Capacitor.isNativePlatform()) return false;
      try { await BiometricAuth.authenticate({ reason: 'Usá tu rostro para desbloquear la sesión', androidTitle: 'Ingresar a la aplicación', cancelTitle: 'Cancelar' }); }
      catch { return false; }
    }
    const guardado = await SecureStorage.get(CLAVE_TOKEN);
    access_token = typeof guardado === 'string' ? guardado : null;
    if (await cargar_usuario()) return true;
    return renovar_sesion();
  })().finally(() => { restauracion = null; restaurando.value = false; });
  return restauracion;
}

export async function cerrar_sesion() {
  if (access_token) await fetch(`${obtener_api_url()}/api/auth/logout`, { method: 'POST', headers: { Authorization: `Bearer ${access_token}` } }).catch(() => undefined);
  await limpiar_sesion();
}

export async function limpiar_sesion() {
  access_token = null; usuario.value = null;
  await Promise.all([SecureStorage.remove(CLAVE_TOKEN), SecureStorage.remove(CLAVE_REFRESH)]);
  localStorage.removeItem(CLAVE_BIOMETRIA);
}

export async function configurar_biometria(activar: boolean) {
  if (activar) {
    const estado = await BiometricAuth.checkBiometry();
    const tiene_rostro = estado.biometryTypes.includes(BiometryType.faceAuthentication) || estado.biometryType === BiometryType.faceId;
    if (!Capacitor.isNativePlatform() || !estado.isAvailable || !tiene_rostro) throw new Error('No hay reconocimiento facial registrado en este dispositivo.');
    await BiometricAuth.authenticate({ reason: 'Confirmá tu rostro para activar el acceso', androidTitle: 'Activar reconocimiento facial' });
  }
  localStorage.setItem(CLAVE_BIOMETRIA, String(activar));
}

export function obtener_access_token() { return access_token; }
export const sesion = {
  usuario, restaurando,
  autenticado: computed(() => Boolean(usuario.value && access_token)),
  biometria_activa: computed(() => localStorage.getItem(CLAVE_BIOMETRIA) === 'true'),
  es_admin: computed(() => usuario.value?.rol === 'administrador'),
};
