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

## Pendiente

- **C1 (crítica, a cargo del responsable de Azure):** el historial público del repositorio contiene la contraseña del usuario SQL de Azure y dos claves de Supabase. Hay que rotar la contraseña (user-secrets y App Settings), revisar el firewall de Azure SQL y revocar las claves de Supabase. Reescribir el historial es opcional.
- M5: el login revela por tiempo de respuesta si un correo existe (no aprobado en esta ronda).
- B2: advertencias de nulabilidad (unas 15).
- B3: el acceso a datos es mayormente síncrono; migrarlo a async solo si la carga lo justifica.
- B4: no hay proyecto de pruebas automatizadas.
- B5: el README está vacío.
- Política CSP completa en la Web (hoy hay scripts y estilos inline).
- Bloqueo de cuenta persistente por `intentos_login`: requiere una columna nueva (por ejemplo, la fecha hasta la que queda bloqueada).
