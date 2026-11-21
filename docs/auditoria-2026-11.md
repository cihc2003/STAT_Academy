# Auditoría técnica — noviembre 2026

Rama `auditoria-seguridad` (desde `origin/master` en `a1463bd`). Sin cambios de esquema de base de datos.

## Implementado

| Id | Prioridad | Cambio | Archivos principales |
|----|-----------|--------|----------------------|
| A1 | Alta | Límite de intentos (rate limiting del framework, sin paquetes). API: por correo en login (10 cada 15 min) y recuperación (3 por hora, sin cambiar la respuesta), por IP en login/registro/recuperación/restablecer (120/min), por usuario en cambio de contraseña (5 cada 15 min) y en reportes de error (20 cada 10 min). Web: por IP del visitante en login (30/min) y en registro/recuperación/restablecer (10 cada 10 min), con aviso en pantalla. | `Api/Seguridad/LimitesSolicitudes.cs`, `Web/Seguridad/LimitesSolicitudesWeb.cs`, `Api/Program.cs`, `Web/Program.cs`, controladores de Login, Contraseña, Usuario, Errores y Cuenta |
| A2 | Alta | Revocación de JWT sin cambiar el esquema: claim `sello` = HMAC (clave del JWT) del hash de la contraseña y el tipo de usuario. Se comprueba en `OnTokenValidated` (caché de 60 s, invalidada al cambiar contraseña, rol o estado). Cambiar la contraseña devuelve un token nuevo (campos `token` y `expiraEn` añadidos a la respuesta de `POST api/Contrasena/cambiar`). | `Api/Auth/ValidadorSesion.cs`, `TokenService.cs`, `LoginService.cs`, `ContrasenaService.cs`, `UsuarioService.cs`, `Web/Controllers/CuentaController.cs`, `Web/Services/ApiContrasenaService.cs` |
| A3 | Alta | Validación en el servidor: `LoginModel`, `ProveedorCreateRequest`, `CursoCreateRequest`, `RegisterUsuarioRequest` (obligatorios y largos de las columnas). Respuesta 400 en lugar de 500. | `Api/Models/LoginModel.cs`, `DTOs/*`, `Web/Models/Admin/CursoAdminViewModels.cs`, `Web/Models/Proveedor/ProveedorCreateRequest.cs` |
| M1 | Media | Carrito y proveedores solo devuelven el mensaje de sus reglas de negocio (`InvalidOperationException`); cualquier otro error va al manejador global (mensaje genérico + traceId). | `CarritoService/Controller`, `ProveedorService/Controller` |
| M2 | Media | Swagger solo en Development o con `Swagger:Habilitado=true` (App Settings `Swagger__Habilitado`). | `Api/Program.cs`, `Api/appsettings.json` |
| M3 | Media | Encabezados: Web `X-Content-Type-Options`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy`; cookie de sesión `Secure` fuera de Development. API `nosniff`, `X-Frame-Options: DENY` y HSTS fuera de Development. | `Api/Program.cs`, `Web/Program.cs` |
| M4 | Media | `fk_creador` del curso sale del token al crear y no cambia al editar (el campo del cuerpo se ignora). | `Api/Services/CursoService.cs`, `Api/Controllers/CursoController.cs` |
| B1 | Baja | Código sin uso eliminado: `LoginResponse.cs` (raíz), `Api/Models/RegistroUsuarioModel.cs`, `Api/Models/Contrasena/*ViewModel.cs`, `Web/Services/CursoService.cs`, `AddSession/UseSession`, carpetas vacías del csproj. | — |

### Efectos a tener en cuenta al desplegar

- Los tokens emitidos antes del despliegue no tienen sello: todos los usuarios deberán iniciar sesión de nuevo una vez.
- Con varias instancias de la API, la revocación tarda como máximo 60 s en las demás instancias, y cada instancia lleva sus propios contadores de intentos.
- En Azure, Swagger deja de verse salvo que se configure `Swagger__Habilitado=true`.

## Segunda ronda

| Id | Cambio | Archivos principales |
|----|--------|----------------------|
| M5 | El login verifica contra un hash ficticio cuando el correo no existe (mismo tiempo de respuesta). | `Api/Services/LoginService.cs` |
| Bloqueo | Tras 5 fallos seguidos la cuenta se bloquea 15 minutos (responde 429 con el mismo mensaje que el límite). Tabla nueva `USUARIO_BLOQUEO`; restablecer la contraseña quita el bloqueo. **Script `Database/Scripts/2026-11-16_usuario_bloqueo.sql` sin aplicar**: mientras no exista la tabla, la API atrapa el error 208 y solo rige el límite en memoria. | `LoginService.cs`, `Models/UsuarioBloqueoModel.cs`, `ContrasenaService.cs` |
| B3 | Async en las rutas más usadas: `CursoService` completo, aula del estudiante y permisos de matrícula (material y tareas). | `CursoService.cs`, `AulaService.cs`, `TutorService.cs`, controladores de Curso, Aula, MaterialCurso y Tarea |
| CSP | Sin JavaScript inline (todo en `site.js` con `data-un-solo-envio`, `data-enviar-al-cambiar`, `data-imprimir`); `Content-Security-Policy` en páginas HTML. | `Web/Program.cs`, `wwwroot/js/site.js`, vistas de Blog, Carrito, Curso, Facturas, Products, Usuario y Error |
| B2 | Sin advertencias de compilación (nulabilidad). | DTOs y modelos |
| B4 | Proyecto `STAT_Academy.Tests` (xUnit) con 17 pruebas. | `STAT_Academy.Tests/*` |
| B5 | README con arquitectura, configuración, ejecución local y convenciones. Título de la pestaña del carrito. | `README.md`, `Views/Carrito/Index.cshtml` |

## Tercera ronda

| Id | Cambio | Archivos principales |
|----|--------|----------------------|
| B3 | Todos los servicios de la API con EF Core async: tutor, blog, productos, proveedores, carrito, compras, matrícula, material, tareas y adjuntos, usuarios y perfil, contraseña, nota final, auditoría (`RegistrarAsync`) y el aviso de errores por correo. Los controladores hacen `await`; las descargas pasan su `CancellationToken` a las consultas. Sin cambios de rutas, JSON ni esquema. | `Api/Services/*`, `Api/Controllers/*`, `Api/Errores/ErrorAlertaService.cs` |
| CSP | Las vistas ya no usan `style=` (clases al final de `site.css`; las barras de progreso usan `data-progreso` en `site.js`). `style-src` sin `'unsafe-inline'`: no se aceptan bloques `<style>` en línea. Solo `style-src-attr 'unsafe-inline'`, porque el contenido del blog guarda colores como atributo (la API los sanea: solo `color` y `background-color`). En navegadores sin `style-src-attr` (anteriores a 2022/2023) esos colores no se aplican. | `Web/Program.cs`, `wwwroot/css/site.css`, `wwwroot/js/site.js`, vistas de Carrito, Curso, Facturas y Tutor |
| B4 | Pruebas de servicios con base de datos: SQLite en memoria con el esquema del modelo de EF (`EnsureCreated`), sin tocar Azure, y Blob Storage simulado en memoria (`BlobStorageSimulado`). 38 pruebas: proveedores, carrito, productos, usuarios y perfil, tutor, compras (transacción, stock y carrito), facturas y pagos (comprobante privado, aprobar con matrícula, rechazar, permisos), blog (saneado, visibilidad, adjuntos e imagen principal) y aula (entregas, retiro, calificada, permisos de descarga). En total 74 pruebas. | `STAT_Academy.Tests/BaseDatos/*` |

## Pendiente

- **C1 (crítica, a cargo del responsable de Azure):** el historial público del repositorio contiene la contraseña del usuario SQL de Azure y dos claves de Supabase. Hay que rotar la contraseña (user-secrets y App Settings), revisar el firewall de Azure SQL y revocar las claves de Supabase. Reescribir el historial es opcional.
- Aplicar `Database/Scripts/2026-11-16_usuario_bloqueo.sql` en Azure para activar el bloqueo persistente.
- Pruebas: SQLite no ordena por columnas `decimal`, así que el orden de facturas por total (`total_desc`/`total`) no está cubierto; tampoco el envío real de correos ni la estrategia de reintento de SQL Server (en SQLite no reintenta).
