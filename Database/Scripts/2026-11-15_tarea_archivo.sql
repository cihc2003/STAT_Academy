/* ============================================================================
   Tareas: documentos adjuntos por el tutor (tabla TAREA_ARCHIVO)
   Fecha: 2026-11-15
   - Idempotente: se puede ejecutar más de una vez sin efectos adicionales.
   - Solo CREA la tabla y su índice; no modifica TAREA ni datos existentes.
   - Guías, plantillas para llenar o enlaces que el tutor adjunta a una tarea.
     Ubicacion guarda el nombre del blob (contenedor privado "documentos", o el de
     imágenes si es una imagen) o la URL completa si Tipo = 'Enlace'.
   - Mientras no se aplique, la API sigue funcionando: la tarea se muestra sin
     adjuntos y el formulario del tutor indica que no están habilitados.
   ========================================================================= */

IF OBJECT_ID('dbo.TAREA_ARCHIVO', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TAREA_ARCHIVO
    (
        ID              INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TAREA_ARCHIVO PRIMARY KEY,
        FK_Tarea        INT NOT NULL
            CONSTRAINT FK_TAREA_ARCHIVO_TAREA REFERENCES dbo.TAREA(ID),
        Titulo          NVARCHAR(255) NOT NULL,
        Ubicacion       VARCHAR(500) NOT NULL,
        Tipo            VARCHAR(50) NOT NULL,
        Estado          BIT NOT NULL
            CONSTRAINT DF_TAREA_ARCHIVO_Estado DEFAULT (1),
        FK_Autor        INT NULL
            CONSTRAINT FK_TAREA_ARCHIVO_AUTOR REFERENCES dbo.USUARIO(ID),
        Fecha_Creacion  DATETIME2(0) NOT NULL
            CONSTRAINT DF_TAREA_ARCHIVO_Fecha_Creacion DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- Los adjuntos siempre se consultan por tarea.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_TAREA_ARCHIVO_FK_Tarea' AND object_id = OBJECT_ID('dbo.TAREA_ARCHIVO'))
    CREATE INDEX IX_TAREA_ARCHIVO_FK_Tarea
        ON dbo.TAREA_ARCHIVO (FK_Tarea)
        INCLUDE (Estado);
GO
