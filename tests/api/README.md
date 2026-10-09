# Pruebas backend de unidad6

```powershell
dotnet test "tests/api/Unidad6.Tests.csproj" --logger "console;verbosity=normal"
```

Las pruebas invocan los controladores con `await`; no arrancan Program, un servidor HTTP ni JWT,
no cargan appsettings, variables de conexion o user-secrets y no usan MySQL ni migraciones.
SQLite vive exclusivamente en memoria, con una conexion independiente por prueba. El fixture
traduce solo el CHECK de estados de MySQL a su equivalente SQLite sensible a mayusculas;
conserva relaciones, borrado SetNull y el token de concurrencia de la API.

QuestPDF Community se configura en el fixture antes de generar un PDF real. Se verifican
firma y fin del PDF (con 1 y 100 renglones y nombres de longitud maxima), PNG del QR,
nombres de descarga, reglas de cantidades e importes,
captura historica del catalogo, estados terminales, concurrencia y errores de persistencia.
Los roles se verifican por reflexion, no mediante el middleware de autorizacion.

Los archivos de fotos solo se generan en subdirectorios GUID de
`C:\Users\Nahu\AppData\Local\Temp\opencode\unidad6-tests` y se eliminan al disponer cada scope.
El directorio aprobado debe existir; las pruebas no usan un directorio temporal alternativo.
No se escriben PDFs ni PNG de QR al disco. `bin/` y `obj/` estan ignorados por git.

Estas pruebas no verifican binding multipart/JSON, JWT, restricciones de precision propias
de MySQL ni el contenido textual o la decodificacion del QR del PDF. No reemplazan pruebas
de integracion HTTP o de despliegue.
