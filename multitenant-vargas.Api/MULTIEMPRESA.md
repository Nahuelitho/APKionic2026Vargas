# Multiempresa

## Permisos

| Cuenta | Catalogo | Pedidos / detalle / QR | Escritura | PDF |
| --- | --- | --- | --- | --- |
| Comun, sin membresias | Todas las empresas activas | Solo sus propios pedidos, en cualquier empresa | Crear pedido de una unica empresa | No |
| Vendedor | Su empresa de sesion | Todos los de su empresa | Productos y estados de pedidos de su empresa | Su empresa, solo Listo |
| Administrador | Su empresa de sesion | Todos los de su empresa | Lo anterior y usuarios que ya pertenecen a su empresa | Su empresa, solo Listo |
| Superadmin global | Todas, incluidas inactivas | Todos | Empresas, productos, pedidos y membresias de usuarios | Todos, solo Listo |
| Caja existente | Su empresa | Todos los de su empresa | Estados de pedidos; sin gestion de productos/usuarios | No |

Los recursos ajenos devuelven 404; operaciones no autorizadas por rol devuelven 403.
El catalogo "publico" requiere autenticacion. No se agrega registro de cuentas.
Un usuario creado sin rol por superadmin es comun, no una cuenta pendiente de habilitacion.
El administrador de empresa no puede buscar clientes globales, adoptar cuentas ajenas,
asignar superadmin, cambiar otras empresas ni modificar su propia autorizacion.
Superadmin puede agregar, cambiar o quitar una membresia por empresa sin alterar las demas.

## Esquema

- `empresas(id, nombre_empresa, activo)`: empresa tiene muchos productos y pedidos. No lleva PedidoId.
- `usuario_roles(id, usuario_id, rol_id, empresa_id NULL)`: un rol por usuario/empresa; NULL solo para superadmin global. Usuario comun no tiene filas.
- IDs de rol reservados: 1 administrador, 2 vendedor, 3 caja, 4 superadmin. CHECK restringe los roles de empresa a EmpresaId no nulo y superadmin a NULL.
- `productos.empresa_id`: obligatorio, FK restrict a empresas; clave alternativa `(id, empresa_id)`.
- `pedidos.empresa_id` y `pedidos.usuario_id`: obligatorios. El servidor toma UsuarioId de la identidad, nunca del cuerpo. FK restrict a empresa y usuario; clave alternativa `(id, empresa_id)`.
- `pedido_items.empresa_id`: obligatorio. FKs compuestas `(pedido_id, empresa_id)` y `(producto_id, empresa_id)` impiden cruces incluso mediante SQL directo.
- Eliminar un producto desvincula ProductoId de sus items en la misma transaccion y conserva EmpresaId, nombre/precios y totales. Eliminacion directa SQL de un producto referenciado esta restringida.
- `refresh_tokens`: incorpora empresa, rol y version de ambito; rotacion con control de concurrencia. JWT incluye ambito y referencia a la sesion. Cada request revalida usuario, membresia, empresa, rol y sesion no revocada. Logout y cambios de membresia invalidan JWT y refresh.
- `usuarios.rol_id` y su FK se conservan como datos legacy para una migracion aditiva. No otorgan permisos ni se actualizan desde la API nueva.

## Migracion Segura

`20261008133552_Multiempresa` fue generada con `dotnet ef` y completada con backfill.
No elimina usuarios, productos, pedidos ni items existentes.
Asigna productos, pedidos e items existentes a Vargas (EmpresaId 1).
Migra todos los roles legacy administrador/vendedor/caja a membresias de Vargas;
sinrol queda comun global. Los pedidos anteriores no registraban autor: quedan a
nombre de un usuario tecnico inactivo con email `historial-<uuid>@vargas.invalid`
y password no utilizable, visibles solo para gestores de Vargas y superadmin.
No se atribuyen a sinrol ni se infiere autor desde el texto Cliente.
Revoca las sesiones antiguas; todos deben iniciar sesion de nuevo.

Requiere MySQL 8.0.16 o posterior para CHECKs efectivos. Hacer backup y probar
primero en una copia. Detener escrituras de la API durante la migracion: MySQL
realiza commits implicitos para DDL. No hacer rollback a esquema viejo despues
de crear datos multitenant, porque perderia el ambito de aislamiento.

Desde `multitenant-vargas.Api`, configurar `ConnectionStrings__Default` para la
base objetivo y `Jwt__Key` fuera del repositorio. Con la API detenida:

```powershell
dotnet tool restore
dotnet build -c TenantVerification -p:UseAppHost=false
dotnet ef migrations script 20260929222830_Unidad6FotosYPedidos 20261008133552_Multiempresa --configuration TenantVerification --no-build --output Migrations/Multiempresa.review.sql
# Revisar el SQL generado antes de aplicar. Para una base nueva, omitir FROM en migrations script.
dotnet ef database update --configuration TenantVerification --no-build
dotnet run
```

No se aplico la migracion a la base en uso. El script de revision se genera solo
cuando se ejecuta el comando anterior y no debe incluir credenciales.

## Cuentas Y Bootstrap

Las cuentas de desarrollo existentes conservan contrasenas y pasan a estos ambitos:

| Email | Password de desarrollo | Ambito |
| --- | --- | --- |
| admin@vargas.com | Admin123! | Administrador de Vargas |
| vendedor@vargas.com | Vendedor123! | Vendedor de Vargas |
| sinrol@vargas.com | SinRol123! | Comun, sin membresias |

No se crea un superadmin con password conocido. El operador de base debe elegir
una cuenta controlada existente con password seguro para el primer superadmin:

```sql
INSERT INTO usuario_roles (usuario_id, rol_id, empresa_id)
SELECT u.id, 4, NULL FROM usuarios u
WHERE u.email = 'cuenta-controlada@su-dominio.com' AND u.activo = 1
AND NOT EXISTS (SELECT 1 FROM usuario_roles ur WHERE ur.usuario_id = u.id AND ur.empresa_id IS NULL);
```

Esto es un paso administrativo manual, no un endpoint publico. Volver a iniciar
sesion despues. No promover cuentas de demostracion en produccion. Los gestores
globales pueden crear nuevas cuentas seguras y otras empresas desde la interfaz.

## Contratos Y UI

Se mantienen `id`, paginacion snake_case, `fotoUrl`, items `productoId` / `precioUnitario`,
rutas de PDF/QR y los cuerpos existentes. Respuestas agregan `empresa_id`,
`nombre_empresa` y, para pedidos, `usuario_id`. El cuerpo Pedido acepta `empresa_id`;
si se omite, infiere la unica empresa de sus productos y rechaza mezclas.
Producto multipart acepta `empresaId`; es obligatorio para superadmin al crear,
los gestores de empresa usan su ambito y no pueden cambiar EmpresaId de un producto.
`fotoUrl` conserva `/uploads/productos/<archivo>`, ahora servido por un endpoint
autenticado con el mismo aislamiento del catalogo, no por archivos estaticos.
Vue obtiene el blob con Authorization y libera la imagen al cambiar de sesion.

`POST /api/auth/login` admite `empresa_id` opcional para multiples membresias.
Sin seleccion, toma la primera empresa activa por ID. Superadmin tiene prioridad
global. Refresh conserva el mismo ambito, nunca lo cambia a otra empresa.
La pantalla de login ofrece el ID opcional; Mi cuenta muestra el ambito de sesion.
Catalogo agrupado por empresa con paginacion tambien para comunes. Nuevo pedido
selecciona empresa y limpia el carrito al cambiarla. Comun ve "Mis pedidos" y no
el boton PDF. Superadmin tiene pantalla Empresas y selector de ambito en Usuarios.

## Verificacion

Desde la raiz:

```powershell
dotnet test tests/api/Unidad6.Tests.csproj -c TenantVerification -p:UseAppHost=false
npm run test:unit -- --run
npm run build
```

Backend incluye pruebas HTTP del middleware JWT real, aislamiento entre dos
empresas, propiedad de pedidos, QR/PDF, escritura de productos, gestion de usuarios,
superadmin, multiples membresias, rotacion/revocacion y CHECK/FKs compuestas.
Pruebas usan SQLite en memoria y archivos temporales; nunca la base real.
Frontend cubre contratos de empresa, agrupacion, paginacion comun, seleccion de
empresa y ocultamiento de PDF/estado. Para probar manualmente, crear segunda empresa
y sus gestores con superadmin, iniciar sesiones separadas y tratar de acceder a IDs
ajenos. Clientes comunes distintos deben ver listas de pedidos disjuntas.

El daemon Docker no estaba disponible, por lo que no se ejecuto la migracion contra
un MySQL descartable. Revisar y ensayar el SQL en una copia antes del despliegue.
