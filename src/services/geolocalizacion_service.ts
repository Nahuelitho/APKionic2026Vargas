import { Capacitor } from '@capacitor/core';
import { Geolocation } from '@capacitor/geolocation';

export type ResultadoUbicacion =
  | { ok: true; ubicacion: { latitud: number; longitud: number; precision: number }; mensaje: null }
  | { ok: false; ubicacion: null; mensaje: string | null };

export async function obtener_ubicacion_actual(): Promise<ResultadoUbicacion> {
  if (!Capacitor.isPluginAvailable('Geolocation')) {
    return {
      ok: false,
      ubicacion: null,
      mensaje: 'La geolocalización no está disponible.',
    };
  }
  try {
    const permisos = await Geolocation.requestPermissions();
    // El usuario puede dar la ubicación precisa (GPS) o la aproximada (antenas y wifi):
    // para anotar la ubicación del local, cualquiera de las dos sirve.
    if (
      permisos.location !== 'granted' &&
      permisos.coarseLocation !== 'granted'
    ) {
      return {
        ok: false,
        ubicacion: null,
        mensaje: 'Sin permiso de ubicación. Habilitalo en los ajustes.',
      };
    }
    const posicion = await Geolocation.getCurrentPosition({
      enableHighAccuracy: true,
      timeout: 12000,
      maximumAge: 60000,
    });
    return {
      ok: true,
      ubicacion: {
        latitud: posicion.coords.latitude,
        longitud: posicion.coords.longitude,
        precision: posicion.coords.accuracy,
      },
      mensaje: null,
    };
  } catch (error) {
    return {
      ok: false,
      ubicacion: null,
      mensaje:
        error instanceof Error && error.message
          ? error.message
          : 'No se pudo obtener la ubicación.',
    };
  }
}

export function formatear_coordenadas(
  latitud: number | null | undefined,
  longitud: number | null | undefined,
): string {
  if (latitud == null || longitud == null) return '';
  return `${Number(latitud).toFixed(6)}, ${Number(longitud).toFixed(6)}`;
}

// El mapa es EXTERNO a propósito: abrir la app de mapas del teléfono con las coordenadas ya
// resuelve la clase. El mapa embebido con marcadores es la versión 10.
// Solo se buscan por COORDENADAS: si la empresa todavía no tiene punto, no se abre nada
// (buscar por nombre mostraba un lugar cualquiera y confundía).
export function url_mapa(
  latitud: number | null | undefined,
  longitud: number | null | undefined,
): string {
  if (latitud == null || longitud == null) return '';
  if (Number.isNaN(Number(latitud)) || Number.isNaN(Number(longitud))) return '';
  return `https://www.google.com/maps/search/?api=1&query=${latitud},${longitud}`;
}

export function tiene_coordenadas(
  latitud: number | null | undefined,
  longitud: number | null | undefined,
): boolean {
  return url_mapa(latitud, longitud) !== '';
}
