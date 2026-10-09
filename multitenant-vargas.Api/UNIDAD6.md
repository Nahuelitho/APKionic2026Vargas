# Unidad 6: contratos backend

Todos los endpoints de API requieren JWT. Los errores usan `{ "codigo": "...", "mensaje": "..." }`.
No se incorporaron tenants, clientes como entidad, ni cambios de infraestructura.

## Permisos

| Operacion | administrador | vendedor | caja |
| --- | --- | --- | --- |
| Leer productos y pedidos, descargar QR/comprobante | Si | Si | Si |
| Crear/editar/eliminar productos | Si | No | No |
| Crear pedidos | Si | Si | No |
| Cambiar estado de pedidos | Si | No | Si |

Las fotos en `/uploads/productos/...` son publicas para el catalogo. Las descargas de pedidos no son publicas.

## Productos

`POST /api/productos` y `PUT /api/productos/{id}` aceptan exclusivamente `multipart/form-data`,
con campos `nombre`, `descripcion` opcional, `precio`, `stock` booleano y archivo `foto` opcional.
POST devuelve 201 con Location; PUT devuelve 200.

Respuesta de producto:

```json
{"id":1,"nombre":"Producto","descripcion":null,"precio":1200.50,"stock":true,"fotoUrl":"/uploads/productos/00000000000000000000000000000000.jpg"}
```

`fotoUrl` es null cuando no hay foto. Los demas contratos de productos se conservan:
`GET /api/productos?pagina=1&tamanio=10` devuelve
`{productos, pagina, tamanio, total, total_paginas}`; tamanio admite 1..50.
`GET /api/productos/{id}` devuelve el producto. DELETE devuelve 204.

Foto: entre 1 byte y 5 MiB (5 * 1024 * 1024 bytes), MIME image/jpeg, image/png o image/webp,
con firma binaria correspondiente. No se usa el nombre original. Se guarda con GUID y extension
derivada del MIME en `wwwroot/uploads/productos`. La solicitud multipart completa admite hasta 6 MiB.
Omitir `foto` conserva la anterior; no hay operacion de quitar foto por separado.
La foto anterior solo se borra despues de SaveChanges exitoso; si SaveChanges falla se limpia
la nueva. DELETE tambien limpia la foto despues de guardar. Errores de borrado se registran sin
convertir una escritura de DB exitosa en un fallo; pueden requerir limpieza operativa posterior.

El nombre admite hasta 160 caracteres, descripcion 1000 y precio positivo, hasta 9999999999.99,
con no mas de dos decimales. `stock` conserva el significado existente de disponibilidad booleana.

## Pedidos

`POST /api/pedidos`:

```json
{"cliente":"Ana Perez","items":[{"productoId":1,"cantidad":2}]}
```

Cliente: nombre de texto obligatorio, se recorta y admite hasta 160 caracteres.
Se requieren entre 1 y 100 items, sin productos duplicados, con IDs positivos y cantidades
enteras entre 1 y 10000. Los productos deben existir y tener stock=true. No se descuenta
inventario numerico porque el modelo existente solo tiene stock booleano.
Nombre y precio se capturan del catalogo, nunca del request. Subtotales y total se calculan
en el servidor y admiten hasta 9999999999.99. Cabecera y renglones se guardan en un unico SaveChanges.
Estado inicial: `En preparacion`. Fecha: UTC. Respuesta 201 con Location y detalle.

`GET /api/pedidos` devuelve `{ "pedidos": [...] }`, sin paginacion, ordenado por ID descendente.
Cada resumen tiene `{id, cliente, fecha, estado, total}`.

`GET /api/pedidos/{id}`, POST y PUT de estado devuelven el mismo detalle:

```json
{
  "id": 1,
  "cliente": "Ana Perez",
  "fecha": "2026-09-29T22:00:00Z",
  "estado": "En preparacion",
  "total": 2401.00,
  "items": [
    {"id":1,"productoId":1,"nombre":"Producto","precioUnitario":1200.50,"cantidad":2,"subtotal":2401.00}
  ]
}
```

Si se elimina un producto, su `productoId` historico pasa a null, pero nombre, precio,
cantidad y subtotal se conservan. Items se ordenan por ID ascendente.

`PUT /api/pedidos/{id}/estado`:

```json
{"estado":"Listo"}
```

Estados exactos y sensibles a mayusculas: `En preparacion`, `Listo`, `Cancelado`.
Solo se puede operar sobre `En preparacion`; enviar ese mismo estado es un no-op.
Pasar a Listo confirma el pedido; pasar a Cancelado lo cancela. Ningun pedido terminal se
edita, ni siquiera reenviando su estado actual. El estado es token de concurrencia EF:
dos confirmaciones/cancelaciones concurrentes no pueden sobrescribirse.

`GET /api/pedidos/{id}/qr`: PNG en cualquier estado, archivo `PED-0001.png`, payload exacto
`PED-{id:D4}` (minimo cuatro digitos, sin truncar IDs mayores).

`GET /api/pedidos/{id}/comprobante`: solo Listo; en cualquier otro estado devuelve 409.
PDF A4, archivo `PED-0001.pdf`, incluye cliente, fecha UTC, estado, renglones, precios,
subtotales, total, paginacion y QR con el mismo payload. Moneda formateada con cultura es-AR.
QuestPDF 2025.7.3 usa licencia Community; verificar elegibilidad de la organizacion antes
de un despliegue comercial. QRCoder 1.6.0 genera PNG sin System.Drawing.

## Errores

| HTTP | Codigo | Caso |
| --- | --- | --- |
| 400 | solicitud_invalida | Binding/JSON/campos obligatorios invalidos |
| 400 | pagina_invalida, tamanio_invalido | Paginacion de productos invalida |
| 400 | nombre_obligatorio, nombre_muy_largo, descripcion_muy_larga, precio_invalido | Validacion de producto |
| 400 | foto_invalida | Tamano, MIME o firma de imagen invalidos |
| 400 | cliente_invalido, items_invalidos, producto_duplicado, importe_invalido | Validacion de pedido |
| 400 | producto_no_encontrado | POST pedido con productos inexistentes |
| 400 | estado_invalido | Estado no pertenece a los tres valores exactos |
| 401 | no_autenticado | JWT faltante o invalido |
| 403 | sin_permiso | Rol no autorizado |
| 404 | producto_no_encontrado, pedido_no_encontrado | Recurso inexistente |
| 409 | producto_sin_stock | Producto no disponible al crear pedido |
| 409 | pedido_terminal | Se intenta modificar Listo/Cancelado |
| 409 | pedido_modificado | Conflicto concurrente de estado |
| 409 | comprobante_no_disponible | Pedido no esta Listo |
| 413 | http_413 | Solicitud supera limite de carga |
| 415 | http_415 | Tipo de contenido no soportado |
| 500 | producto_no_guardado, pedido_no_guardado | Fallo controlado de persistencia |
| 500 | error_interno | Otro fallo de procesamiento, incluyendo almacenamiento/PDF |

Otros errores HTTP sin cuerpo reciben `http_{status}` con mensaje generico.

## Persistencia y verificacion

Migracion generada por `dotnet ef`: `20260929222830_Unidad6FotosYPedidos`, con Designer y snapshot.
Agrega productos.foto_url, pedidos, pedido_items, indices, constraint de estados y relaciones.
La fabrica de diseno carga appsettings y variables de entorno como la API, con version
explicita MySQL: no necesita arrancar la API ni contactar una DB para generar migraciones.
No se ejecuto database update. Aplicar la migracion queda a cargo del entorno de despliegue.
El proceso de API necesita permisos de escritura en wwwroot/uploads/productos.
