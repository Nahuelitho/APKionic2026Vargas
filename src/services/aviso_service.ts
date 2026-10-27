import { toastController } from '@ionic/vue';

export async function avisar(mensaje: string, color: 'success' | 'danger' = 'success') {
  try {
    const toast = await toastController.create({ message: mensaje, duration: 2200, color, position: 'top' });
    await toast.present();
  } catch (e) {
    // Un toast fallido nunca debe cortar el flujo que lo disparo.
    console.warn('[aviso] No se pudo mostrar el toast', e);
  }
}