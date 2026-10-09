import { Haptics, NotificationType } from '@capacitor/haptics';

const CLAVE_VIBRACION = 'vibracion_activa';

export function vibracion_activada(): boolean {
  return localStorage.getItem(CLAVE_VIBRACION) !== 'false';
}

export function configurar_vibracion(activa: boolean): void {
  localStorage.setItem(CLAVE_VIBRACION, String(activa));
}

export async function vibrar_error(): Promise<void> {
  if (!vibracion_activada()) return;
  try {
    await Haptics.notification({ type: NotificationType.Error });
  } catch {
    // La falta de motor de vibracion no debe impedir la operacion.
  }
}
