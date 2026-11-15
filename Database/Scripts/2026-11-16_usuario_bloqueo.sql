/* ============================================================================
   Seguridad: bloqueo temporal de cuentas tras varios intentos fallidos (tabla USUARIO_BLOQUEO)
   Fecha: 2026-11-16
   - Idempotente: se puede ejecutar más de una vez sin efectos adicionales.
   - Solo CREA la tabla; no modifica USUARIO ni datos existentes.
   - Una fila como máximo por usuario (FK_Usuario es la clave primaria). La API la crea o
     actualiza cuando USUARIO.Intentos_Login llega a 5 fallos seguidos y bloquea la cuenta
     15 minutos; restablecer la contraseña por correo quita el bloqueo.
   - Mientras no se aplique, la API sigue funcionando: solo queda el límite en memoria
     por correo (10 intentos cada 15 minutos).
   ========================================================================= */

IF OBJECT_ID('dbo.USUARIO_BLOQUEO', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.USUARIO_BLOQUEO
    (
        FK_Usuario       INT NOT NULL
            CONSTRAINT PK_USUARIO_BLOQUEO PRIMARY KEY
            CONSTRAINT FK_USUARIO_BLOQUEO_USUARIO REFERENCES dbo.USUARIO(ID),
        Bloqueado_Hasta  DATETIME2(0) NOT NULL,
        Fecha_Edicion    DATETIME2(0) NOT NULL
            CONSTRAINT DF_USUARIO_BLOQUEO_Fecha_Edicion DEFAULT (SYSUTCDATETIME())
    );
END
GO
