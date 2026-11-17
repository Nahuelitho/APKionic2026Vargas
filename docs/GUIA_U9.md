# Guía U9 — Pulido visual, tabla de teléfonos, push y release

Esta guía cubre todo lo agregado en la U9 y los pasos manuales que quedan fuera del código:
cuenta de servicio, variables del hosting, migración, APK de release y matriz de pruebas.

---

## 1. Qué se implementó

| Área | Archivos principales |
| --- | --- |
| Tokens de diseño (radio, sombra, grises, espacios, tipografía y colores de estado) | `src/theme/variables.css` |
| Tabla única de estados (texto + color + ícono) | `src/config/estados.ts` |
| Piezas reutilizables: aviso, estado, esqueleto, lista y hoja de detalle | `src/components/base/comp-aviso.vue`, `comp-estado.vue`, `comp-esqueleto.vue`, `comp-lista.vue`, `comp-detalle-hoja.vue` |
| Pantallas normalizadas y con jerarquía | `src/views/*.vue`, `src/components/comp-pedido-formulario.vue`, `comp-empresa-editor.vue` |
| Tabla de teléfonos (token FCM por usuario) | `Domain/Entities/Dispositivo.cs`, `Controllers/DispositivosController.cs`, migración `20261117033306_DispositivosPush` |
| Envío de push después de guardar el aviso | `Controllers/PedidosController.cs`, `Services/PushService.cs` |
| Registro/baja del token desde la app | `src/services/push_service.ts`, `src/services/dispositivos_service.ts`, `src/services/auth_service.ts`, `src/App.vue` |
| Canal de importancia alta e ícono | `android/app/src/main/java/com/vargas/multitiendas/MainActivity.java`, `AndroidManifest.xml`, `res/values/colors.xml` |
| Deep link al pedido | `src/services/push_service.ts` + `src/views/PedidosPage.vue` (`?pedido=ID`) |

---

## 2. Cuenta de servicio de Firebase (secreto)

### 2.1 Descargarla

1. Firebase Console → **Configuración del proyecto** → **Cuentas de servicio**.
2. **Generar nueva clave privada** → se descarga un `.json`.
3. Ese archivo permite mandar push en nombre de la app. **No se versiona nunca.**

El `.gitignore` ya bloquea:

```text
**/google-services.json
**/firebase-adminsdk-*.json
**/ServiceAccountKey.json
**/*service-account*.json
```

### 2.2 Dónde guardarla

Guardala **fuera de la carpeta del repositorio**, por ejemplo:

```text
C:\secrets\vargas-service-account.json
```

### 2.3 Configurarla en desarrollo (User Secrets)

El proyecto ya tiene `UserSecretsId`. Desde la carpeta de la API:

```powershell
cd multitenant-vargas.Api

dotnet user-secrets set "Firebase:ProjectId" "tu-proyecto-firebase"
dotnet user-secrets set "Firebase:CredentialsPath" "C:\secrets\vargas-service-account.json"
```

Alternativa: guardar el JSON completo (una sola línea) en User Secrets:

```powershell
$json = Get-Content "C:\secrets\vargas-service-account.json" -Raw | ConvertFrom-Json | ConvertTo-Json -Compress
dotnet user-secrets set "Firebase:CredentialsJson" $json
```

También se acepta la variable estándar `GOOGLE_APPLICATION_CREDENTIALS` apuntando al archivo.

### 2.4 Orden de búsqueda de credenciales

`PushService` intenta, en este orden:

1. `Firebase:CredentialsJson` (JSON completo).
2. `Firebase:CredentialsPath` (ruta a un archivo).
3. `GOOGLE_APPLICATION_CREDENTIALS`.

Si no encuentra ninguna, **la app sigue funcionando**: solo se registra un warning y las push quedan deshabilitadas. Los avisos in-app no se ven afectados.

### 2.5 Configurarla en producción (Render u otro hosting)

Opción A — archivo secreto montado:

- Subir el JSON como **Secret File** en el panel del hosting.
- Render lo monta en `/etc/secrets/<nombre-del-archivo>`.
- Variable de entorno:

```text
Firebase__CredentialsPath = /etc/secrets/vargas-service-account.json
Firebase__ProjectId       = tu-proyecto-firebase
```

Opción B — JSON en variable de entorno (una sola línea):

```text
Firebase__CredentialsJson = {"type":"service_account","project_id":"...","private_key":"..."}
Firebase__ProjectId       = tu-proyecto-firebase
```

> En .NET los dos puntos de configuración se reemplazan por doble guion bajo:
> `Firebase:CredentialsPath` → `Firebase__CredentialsPath`.

**Nunca** subas el JSON al repositorio ni lo dejes dentro de `publish/`.

---

## 3. Variables de entorno de la API publicada

| Variable | Obligatoria | Ejemplo / nota |
| --- | --- | --- |
| `ConnectionStrings__BaseDeDatos` | Sí | `Server=...;Port=3306;Database=...;User=...;Password=...;` |
| `Jwt__Key` | Sí | Mínimo 32 caracteres. Fuera de Development la API no arranca sin ella. |
| `Jwt__Issuer` | Sí | `TuTiendaCerca` |
| `Jwt__Audience` | Sí | `TuTiendaCercaApp` |
| `Admin__Password` | No | Opcional. Si se configura (mínimo 8 caracteres), reemplaza la clave de `admin@vargas.com` al arrancar. Si no, se conserva la clave que ya tiene la base (`Admin123!` del seed). |
| `Cors__AllowedOrigins__0` | Recomendada | Origen permitido, por ejemplo `https://tu-frontend.com`. |
| `Firebase__ProjectId` | Para push | ID del proyecto Firebase. |
| `Firebase__CredentialsPath` o `Firebase__CredentialsJson` | Para push | Ver sección 2. |
| `ASPNETCORE_URLS` | No | El Dockerfile ya usa `http://+:8080`. |

La API aplica las migraciones al arrancar (`db.Database.MigrateAsync()`), así que el despliegue crea la tabla `dispositivos` automáticamente. Antes de publicar, conviene hacer un backup de la base.

Para aplicar la migración manualmente en local:

```powershell
cd multitenant-vargas.Api
dotnet ef database update
```

---

## 4. Endpoints de la tabla de teléfonos

Todos requieren JWT (`Authorization: Bearer ...`).

### Registrar o actualizar el teléfono

```http
POST /api/dispositivos
Content-Type: application/json

{
  "token": "fcm-token-del-telefono",
  "plataforma": "android"
}
```

- El token es único en toda la tabla.
- Si el mismo teléfono inicia sesión con otro usuario, el token se reasigna al usuario actual.
- Responde `204 No Content`.

### Dar de baja un teléfono

```http
DELETE /api/dispositivos/{token}
```

Solo borra el token si pertenece al usuario autenticado.

### Dar de baja todos los teléfonos del usuario

```http
DELETE /api/dispositivos
```

### Verificación en base

```sql
SELECT id, usuario_id, plataforma, creado_en, actualizado_en
FROM dispositivos;
```

El token completo no conviene mostrarlo en capturas.

---

## 5. Flujo completo de la push

1. La app arranca y `preparar_push()` pide permiso, registra los listeners y llama a `PushNotifications.register()`.
2. Firebase entrega el token en el evento `registration`.
3. `push_service.ts` guarda el token en almacenamiento seguro y llama a `POST /api/dispositivos`.
4. Cuando se crea un pedido o cambia su estado:
   - primero se guarda la notificación in-app;
   - después `PedidosController.EnviarPushAsync()` busca los tokens de los destinatarios y llama a `PushService`.
5. `PushService` envía un `MulticastMessage` con:
   - `data.tipo = "pedido"`
   - `data.pedido_id = "<id>"`
   - prioridad alta, TTL 12 h, canal `vargas_importantes`, ícono y color propios.
6. Si Firebase responde `Unregistered` o `InvalidArgument`, el token se borra de la tabla para no reintentar siempre.
7. Al cerrar sesión:
   - la app llama a `DELETE /api/dispositivos/{token}`;
   - borra el token local;
   - ejecuta `PushNotifications.unregister()`;
   - recién después revoca la sesión.

Con la app **abierta**, Android no dibuja sola la push: la app escucha `pushNotificationReceived` y muestra una notificación local con el mismo canal e ícono. Con la app en **segundo plano o cerrada**, la dibuja el sistema.

---

## 6. Deep link al dato que cambió

La push lleva el identificador del pedido y la app navega a:

```text
/app/pedidos?pedido=123
```

- `PedidosPage.vue` lee `route.query.pedido`, valida que sea un entero positivo y abre la hoja de detalle con `abrir_detalle(id)`.
- Si la app estaba cerrada, el toque puede llegar antes de que el router y la sesión estén listos. En ese caso la ruta queda pendiente (`ruta_pendiente`) y `App.vue` la ejecuta cuando `sesion.autenticado` pasa a `true`.
- Si no hay sesión, la navegación queda pendiente hasta que el usuario ingrese.
- El mismo comportamiento aplica al toque de una notificación local (app abierta).

---

## 7. Canal de importancia alta e ícono

- Canal: `vargas_importantes` (`IMPORTANCE_HIGH`, vibración, luz y badge).
- Se crea en `MainActivity.onCreate()` y debe coincidir con:
  - `PushService.CanalAndroid`;
  - `com.google.firebase.messaging.default_notification_channel_id` del manifest.
- Ícono de barra de estado: `@drawable/ic_stat_icon_config_sample` (vector blanco).
- Color de acento: `@color/notification_color` = `#0B6BCB`.
- Si el usuario ya tenía el canal creado, Android conserva su configuración; para probar de cero se puede desinstalar y volver a instalar la app.

---

## 8. APK de release

### 8.1 Requisitos previos

1. `android/app/google-services.json` (del proyecto Firebase, no versionado).
2. Firma: `android/keystore.properties` (no versionado) con:

```properties
storeFile=app/release.keystore
storePassword=********
keyAlias=vargas
keyPassword=********
```

La ruta de `storeFile` es relativa a la carpeta `android/`.
Si el archivo no existe, el build genera un APK sin firmar.

3. `.env.release` presente. Ya deja:

```dotenv
VITE_DEBUG_ACTIVADO=false
VITE_API_URL=
VITE_API_URL_DEBUG=
```

Así el APK usa la URL pública de `src/config/debug.ts`
(`https://apkionic2026vargas.onrender.com`).

### 8.2 Compilar

```powershell
npx vue-tsc --noEmit
npm run build:release
npx cap sync android

cd android
.\gradlew assembleRelease
```

Salida:

```text
android/app/build/outputs/apk/release/app-release.apk
```

Si no hay keystore, el archivo puede llamarse `app-release-unsigned.apk`.

### 8.3 Instalar en el teléfono físico

```powershell
adb install -r android/app/build/outputs/apk/release/app-release.apk
```

O copiar el APK al teléfono y abrirlo permitiendo “instalar apps desconocidas”.

> Para que la push funcione, el APK debe estar firmado con el mismo `applicationId`
> (`com.vargas.multitiendas`) y usar el `google-services.json` del mismo proyecto Firebase.

---

## 9. Matriz de pruebas para el video

### 9.1 Antes y después de dos pantallas

Se recomienda mostrar:

1. **Login:** antes (card simple, sin jerarquía) → después (marca, título, campos con motivo de obligatoriedad y aviso de error).
2. **Pedidos:** antes (lista plana y detalle en la misma pantalla) → después (resumen, estados con ícono, hoja de detalle, confirmaciones).

Opcionales: Inicio (indicadores reales) y Notificaciones (bandeja con estados).

### 9.2 Push con la app abierta

1. Iniciar sesión en el teléfono con un usuario que reciba avisos (por ejemplo `sinrol@vargas.com` para el cambio de estado de su pedido).
2. Crear un pedido desde ese usuario.
3. Desde otro dispositivo o desde Swagger/Postman con un usuario administrador, cambiar el pedido a `Listo`.
4. Verificar que aparece la notificación local, suena/vibra y al tocarla abre `/app/pedidos?pedido=ID` con la hoja del pedido.

### 9.3 Push en segundo plano

1. Dejar la app abierta y presionar Inicio (sin cerrarla).
2. Repetir el cambio de estado.
3. Verificar la push del sistema y el deep link al tocarla.

### 9.4 Push con la app cerrada

1. Deslizar la app fuera de recientes (swipe; **no** usar “Forzar detención”, que bloquea FCM hasta reabrir).
2. Repetir el cambio de estado.
3. Verificar que llega la push y que al tocarla la app arranca y abre el pedido.

### 9.5 Después de Salir no llega nada

1. Cerrar sesión desde **Mi cuenta** (confirmar el diálogo).
2. Verificar en la API que el token ya no existe:

```sql
SELECT COUNT(*) FROM dispositivos WHERE usuario_id = <id>;
```

3. Cambiar el estado de un pedido de ese usuario.
4. Confirmar que **no** llega ninguna push al teléfono.
5. Volver a iniciar sesión y confirmar que el teléfono se registra de nuevo (nueva fila en `dispositivos`).

### 9.6 Datos del dominio en el video

Mostrar que las pantallas usan datos reales de la API: pedidos con código `PED-####`, clientes, empresas y estados del dominio, no datos de ejemplo.

---

## 10. Diagnóstico rápido

| Síntoma | Qué revisar |
| --- | --- |
| No llega ninguna push | Log de la API: `Firebase sin credenciales`. Configurar `Firebase__CredentialsJson` o `Firebase__CredentialsPath`. |
| La API dice que envió pero no llega | Verificar que exista una fila en `dispositivos` para el usuario destinatario. |
| No hay fila en `dispositivos` | Revisar permiso de notificaciones, `google-services.json`, `applicationId` y logs `[push]` en Logcat. |
| Llega sin sonido ni banner | Verificar que el manifest apunte a `vargas_importantes` y que el canal sea `IMPORTANCE_HIGH`. Desinstalar y reinstalar para recrear el canal. |
| Al tocar la push no abre el pedido | Confirmar que el payload trae `data.pedido_id` y que la app tiene sesión iniciada. |
| Después de Salir sigue llegando | Confirmar que el logout terminó (token borrado en la API). Si no hay red, la baja puede fallar: revisar el log y reintentar. |
| El APK de release apunta a localhost | Se compiló con `npm run build` en vez de `npm run build:release`, o falta `.env.release`. |
| Error de google-services en Gradle | Falta `android/app/google-services.json` o el plugin se aplicó sin el archivo. |

---

## 11. Checklist de entrega

- [ ] Cuenta de servicio fuera del repositorio (User Secrets en local, variable/secret file en producción).
- [ ] API publicada con `ConnectionStrings__BaseDeDatos`, `Jwt__Key` y `Firebase__*` (`Admin__Password` es opcional).
- [ ] Migración `DispositivosPush` aplicada (tabla `dispositivos`).
- [ ] APK de release firmado e instalado en el teléfono físico.
- [ ] Video: app abierta desde el ícono, datos reales del dominio.
- [ ] Video: antes/después de dos pantallas mejoradas.
- [ ] Video: push con app abierta, en segundo plano y cerrada.
- [ ] Video: toque de la push abre el pedido que cambió.
- [ ] Video: después de Salir no llega ninguna push.
