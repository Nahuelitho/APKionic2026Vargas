package com.vargas.multitiendas;

import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.graphics.Color;
import android.os.Build;
import android.os.Bundle;

import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity {
    /** Debe coincidir con PushService.CanalAndroid y androidMetaData del manifest. */
    public static final String CANAL_IMPORTANTE = "vargas_importantes";

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        crearCanalImportante();
    }

    /**
     * Canal de importancia alta: suena, vibra y aparece en pantalla.
     * Se crea al abrir la app; si ya existe, Android conserva la configuracion
     * que haya elegido el usuario.
     */
    private void crearCanalImportante() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return;

        NotificationManager manager = getSystemService(NotificationManager.class);
        if (manager == null) return;

        NotificationChannel canal = new NotificationChannel(
                CANAL_IMPORTANTE,
                getString(R.string.notification_channel_name),
                NotificationManager.IMPORTANCE_HIGH);
        canal.setDescription(getString(R.string.notification_channel_description));
        canal.enableVibration(true);
        canal.setVibrationPattern(new long[]{0, 250, 150, 250});
        canal.enableLights(true);
        canal.setLightColor(Color.parseColor("#0B6BCB"));
        canal.setShowBadge(true);
        canal.setBypassDnd(false);
        manager.createNotificationChannel(canal);
    }
}
