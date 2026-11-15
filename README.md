# STAT Academy

Plataforma de cursos, tienda y blog de STAT Academy (texto en español de Costa Rica).

## Proyectos

| Proyecto | Qué es |
|----------|--------|
| `STAT_Academy.Api` | API REST (.NET 10). JWT, EF Core sobre Azure SQL, archivos en Azure Blob Storage, correos con MailKit. |
| `STAT_Academy.Web` | Sitio ASP.NET MVC. No toca la base: llama a la API con `HttpClient` y reenvía el JWT guardado en la cookie de sesión (`BearerTokenHandler`). |
| `STAT_Academy.DTOs` | Contratos compartidos (peticiones y respuestas). |
| `STAT_Academy.Tests` | Pruebas unitarias (xUnit) de la lógica sin base de datos. |

Roles: Admin (1), Tutor (2) y Cliente (3, estudiante).

```
Navegador ──cookie──▶ Web (MVC) ──JWT──▶ API ──▶ Azure SQL
                                           └────▶ Azure Blob Storage (imágenes públicas, documentos privados)
```

## Configuración

Los secretos **nunca** van en el repositorio.

- **Local:** user-secrets de la API (`dotnet user-secrets set ... --project STAT_Academy.Api`):
  - `ConnectionStrings:DefaultConnection`
  - `Jwt:Key` (Base64, mínimo 32 bytes)
  - `Correo:Servidor`, `Correo:Puerto`, `Correo:Usuario`, `Correo:Clave`, `Correo:Remitente`
  - `Web:BaseUrl` (para los enlaces de los correos)
- **Azure:** App Settings del App Service (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Correo__Clave`, ...).
- Blob Storage no usa claves: en local tu cuenta de Visual Studio o `az login`; en Azure la Managed Identity.
- La Web solo necesita `ApiSettings:BaseUrl`.
- Swagger se ve en Development; en Azure solo con `Swagger__Habilitado=true`.

## Ejecutar en local

Los puertos 5256 (API) y 5115 (Web) son los de los perfiles de Visual Studio. Para pruebas en paralelo:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project STAT_Academy.Api --no-launch-profile --urls http://localhost:5456
```

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project STAT_Academy.Web --no-launch-profile --urls http://localhost:5315 --ApiSettings:BaseUrl=http://localhost:5456/
```

Pruebas:

```bash
dotnet test
```

## Base de datos

- Esquema inicial: `STAT_Academy_DB.sql`.
- Cambios posteriores: `Database/Scripts/AAAA-MM-DD_descripcion.sql`, idempotentes. **Los aplica el responsable de la base**, no la aplicación (no hay migraciones de EF).
- Si una tabla nueva todavía no existe, la API atrapa el error 208 de SQL y sigue funcionando sin esa función (ver cada script).

## Seguridad

- La API exige token en todo salvo lo marcado `[AllowAnonymous]`; la identidad sale siempre del token.
- Los tokens llevan un sello: cambiar la contraseña, el rol o desactivar la cuenta los revoca (`Auth/ValidadorSesion.cs`).
- Límites de intentos en login, registro y recuperación de contraseña (API por correo y por IP; Web por IP). Tras 5 fallos seguidos la cuenta se bloquea 15 minutos.
- La Web envía CSP sin JavaScript inline: los comportamientos van en `wwwroot/js/site.js` con atributos `data-*`.
- Detalle de la última auditoría: `docs/auditoria-2026-11.md`.

## Convenciones

- Estilos: variables de `wwwroot/css/site.css`; CSS y JS nuevos al final de `site.css` y `site.js`.
- Confirmaciones con `_ConfirmarModal` (`data-confirmar`), nunca `confirm()`.
- No mostrar ids internos en pantalla.
