# Multitenant Vargas

Aplicación móvil híbrida para la gestión de múltiples empresas (tiendas) bajo un mismo sistema. Permite administrar productos, pedidos, usuarios y notificaciones con aislamiento por empresa y vistas diferenciadas por rol.

## Tecnologías

- **Frontend:** [Vue 3](https://vuejs.org/) + [TypeScript](https://www.typescriptlang.org/) + [Ionic Framework](https://ionicframework.com/)
- **Nativo/Hybrid:** [Capacitor](https://capacitorjs.com/)
- **Backend:** [ASP.NET Core 8](https://dotnet.microsoft.com/) Web API + Entity Framework Core 9
- **Base de datos:** MySQL
- **Notificaciones:** Firebase Cloud Messaging (FCM)
- **Autenticación:** JWT (Access Token + Refresh Token)

## Características principales

- **Multi-empresa / Multi-tenant:** Aislamiento estricto por empresa con `AmbitoService`
- **Roles y permisos:** `superadmin`, `administrador`, `vendedor`, `caja` y usuario sin rol
- **Gestión completa:** Empresas, productos, clientes, pedidos y usuarios
- **Notificaciones in-app:** Con polling y contador en la campanita
- **Push Notifications:** FCM con canal de importancia alta, ícono propio y deep link al pedido que cambió
- **Tabla de teléfonos:** El token se registra al iniciar sesión y se borra al salir
- **Diseño normalizado:** Tokens de radio, sombra, grises y tipografía; estados con texto, color e ícono
- **Piezas reutilizables:** Avisos, esqueletos, listas con estados y hoja de detalle
- **Catálogo por tiendas:** Para usuarios sin rol
- **Modo offline:** Cola de cambios de estado con sincronización al volver la red

## Estructura del proyecto

```text
multitenant-vargas/
├── src/                    # Frontend Vue + Ionic
├── multitenant-vargas.Api/ # Backend ASP.NET Core
├── android/               # Proyecto Android (Capacitor)
├── public/                # Recursos estáticos
└── tests/                 # Pruebas
```

## Instalación y desarrollo

### Frontend

```bash
npm install
npm run dev
```

### Backend

```bash
cd multitenant-vargas.Api
dotnet restore
dotnet run
```

### Android (nativo)

```bash
npm run build
npx cap sync android
npx cap run android
```

## Configuración

- `google-services.json` debe colocarse en `android/app/` (no versionado)
- `android/keystore.properties` para firma de release (no versionado, va en `android/`)
- `.env.release` define el modo de producción del APK (sin secretos)
- La **cuenta de servicio de Firebase** va en User Secrets en desarrollo y en el panel del hosting en producción; **nunca** en el repositorio
- Variables de la API: `ConnectionStrings__BaseDeDatos`, `Jwt__Key`, `Cors__AllowedOrigins__0` y `Firebase__CredentialsJson` (o `Firebase__CredentialsPath`). `Admin__Password` es opcional y solo sirve para cambiar la clave del administrador al arrancar.
- Ver `appsettings.json` en la API para configuración de base de datos

## Documentación

- [Guía U9](docs/GUIA_U9.md): cuenta de servicio, variables del hosting, endpoints de dispositivos, flujo de push, deep link, APK de release y matriz de pruebas.

## Roles de prueba

| Email | Contraseña | Rol |
| --- | --- | --- |
| `admin@vargas.com` | `Admin123!` | Administrador de Vargas |
| `vendedor@vargas.com` | `Vendedor123!` | Vendedor de Vargas |
| `sinrol@vargas.com` | `SinRol123!` | Usuario sin rol |
| `superadmin@vargas.com` | `SuperAdmin123!` | Superadmin global (todas las empresas) |

## Estado

- **Etapa 7:** Completada - Firebase FCM, notificaciones in-app/push, vistas corregidas
- **Etapa 8:** Completada - Modo offline con cola, firma APK, publicación API
- **Etapa 9:** Completada - Normalización visual, piezas reutilizables, tabla de teléfonos, push con deep link y canal de importancia alta

## Licencia

Proyecto académico - Desarrollo de Aplicaciones Móviles ULP
