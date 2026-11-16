/* ============================================================================
   Registro: aceptación de términos y condiciones y política de cookies (tabla USUARIO_TERMINOS)
   Fecha: 2026-11-16
   - Idempotente: se puede ejecutar más de una vez sin efectos adicionales.
   - Solo CREA la tabla; no modifica USUARIO ni datos existentes.
   - Una fila por usuario (FK_Usuario es la clave primaria) con la versión aceptada y la
     fecha/hora UTC de aceptación. La API la escribe al registrar una cuenta.
   - Mientras no se aplique, el registro sigue funcionando: la API deja un aviso en el log
     y no guarda la aceptación.
   ========================================================================= */

IF OBJECT_ID('dbo.USUARIO_TERMINOS', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.USUARIO_TERMINOS
    (
        FK_Usuario        INT NOT NULL
            CONSTRAINT PK_USUARIO_TERMINOS PRIMARY KEY
            CONSTRAINT FK_USUARIO_TERMINOS_USUARIO REFERENCES dbo.USUARIO(ID),
        Version_Terminos  NVARCHAR(20) NOT NULL,
        Fecha_Aceptacion  DATETIME2(0) NOT NULL
            CONSTRAINT DF_USUARIO_TERMINOS_Fecha_Aceptacion DEFAULT (SYSUTCDATETIME())
    );
END
GO
