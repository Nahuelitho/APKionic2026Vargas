# Multitenant Vargas

Aplicación móvil híbrida para la gestión de múltiples empresas (tiendas) bajo un mismo sistema. Permite administrar productos, pedidos, usuarios y notificaciones con aislamiento por empresa y vistas diferenciadas por rol.

## Tecnologías

- **Frontend:** [Vue 3](https://vuejs.org/) + [TypeScript](https://www.typescriptlang.org/) + [Ionic Framework](https://ionicframework.com/)
- **Nativo/Hybrid:** [Capacitor](https://capacitorjs.com/)
- **Backend:** [ASP.NET Core 10](https://dotnet.microsoft.com/) Web API + Entity Framework Core
- **Base de datos:** MySQL
- **Notificaciones:** Firebase Cloud Messaging (FCM)
- **Autenticación:** JWT (Access Token + Refresh Token)

## Características principales

- **Multi-empresa / Multi-tenant:** Aislamiento estricto por empresa con `AmbitoService`
- **Roles y permisos:** `superadmin`, `administrador`, `vendedor`, `caja` y usuario sin rol
- **Gestión completa:** Empresas, productos, clientes, pedidos y usuarios
- **Notificaciones in-app:** Con polling y contador en la campanita
- **Push Notifications:** Integración con Firebase (FCM)
- **Catálogo por tiendas:** Para usuarios sin rol
- **Listo para modo offline:** En preparación para Etapa 8

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
- `android/keystore.properties` para firma de release (no versionado)
- `.env` / `.env.release` para configuración de URLs (no versionados)
- Ver `appsettings.json` en la API para configuración de base de datos

## Roles de prueba

Consultar el archivo de migraciones o seed para usuarios de desarrollo.

## Estado

- **Etapa 7:** Completada - Firebase FCM, notificaciones in-app/push, vistas corregidas
- **Etapa 8:** En preparación - Modo offline con cola, firma APK, publicación API

## Licencia

Proyecto académico - Desarrollo de Aplicaciones Móviles ULP
