import { Capacitor } from '@capacitor/core';
import { LocalNotifications } from '@capacitor/local-notifications';
import { PushNotifications } from '@capacitor/push-notifications';

export async function notificar_local(titulo: string, cuerpo: string, id = Date.now()) {
  if (!Capacitor.isNativePlatform()) {
    console.log('[local-notif]', titulo, cuerpo);
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

    await LocalNotifications.schedule({
      notifications: [
        {
          title: titulo,
          body: cuerpo,
          id: Number(String(id).slice(-9)),
          schedule: { at: new Date(Date.now() + 150) },
          smallIcon: 'ic_stat_icon_config_sample',
          iconColor: '#488AFF',
        },
      ],
    });
  } catch (e) {
    console.warn('[local-notif] Error al mostrar notificación local', e);
  }
}

let pushInicializado = false;

export async function preparar_push() {
  if (pushInicializado) return;
  if (import.meta.env.VITE_PUSH_NOTIFICATIONS_ENABLED === 'false') return;
  if (!Capacitor.isNativePlatform()) return;
  if (!Capacitor.isPluginAvailable('PushNotifications')) return;

  try {
    const permiso = await PushNotifications.requestPermissions();
    if (permiso.receive !== 'granted') {
      console.warn('[push] Sin permiso para recibir push notifications');
      return;
    }

    // Los listeners van ANTES de register(): si el token llega rápido, no se pierde.
    await PushNotifications.addListener('registration', (token) => {
      console.info('[push] FCM TOKEN:', token.value);
      // Después mandamos este token al backend
    });

    await PushNotifications.addListener('registrationError', (error) => {
      console.warn('[push] Error al registrar en FCM:', error);
    });

    await PushNotifications.addListener('pushNotificationReceived', (notification) => {
      console.log('[push] Recibida con app en primer plano:', notification);
    });

    await PushNotifications.addListener('pushNotificationActionPerformed', (action) => {
      console.log('[push] Acción al tocar la push:', action);
    });

    pushInicializado = true;
    console.log('[push] Push inicializado correctamente');
    await PushNotifications.register();
  } catch (e) {
    console.warn('[push] No se pudo inicializar PushNotifications', e);
  }
}