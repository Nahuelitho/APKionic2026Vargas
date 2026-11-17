import { Capacitor } from '@capacitor/core';
import { LocalNotifications } from '@capacitor/local-notifications';

export const CANAL_NOTIFICACIONES = 'vargas_importantes';

/**
 * Notificacion local: se usa cuando la push llega con la app abierta,
 * porque Android no dibuja solo el mensaje en primer plano.
 */
export async function notificar_local(
  titulo: string,
  cuerpo: string,
  id: string | number = Date.now(),
  datos: Record<string, unknown> = {},
) {
  if (!Capacitor.isNativePlatform()) {
    console.log('[local-notif]', titulo, cuerpo, datos);
    return;
  }

  try {
    const perm = await LocalNotifications.checkPermissions();
    if (perm.display !== 'granted') {
      const req = await LocalNotifications.requestPermissions();
      if (req.display !== 'granted') {
        console.warn('[local-notif] Sin permiso para notificaciones locales');
        return;
      }
    }

    const id_numerico = typeof id === 'number' ? id : Number(String(id).replace(/\D/g, '').slice(-9)) || Date.now();

    await LocalNotifications.schedule({
      notifications: [
        {
          title: titulo,
          body: cuerpo,
          id: id_numerico,
          channelId: CANAL_NOTIFICACIONES,
          schedule: { at: new Date(Date.now() + 150) },
          smallIcon: 'ic_stat_icon_config_sample',
          iconColor: '#0B6BCB',
          extra: datos,
        },
      ],
    });
  } catch (e) {
    console.warn('[local-notif] Error al mostrar notificación local', e);
  }
}
