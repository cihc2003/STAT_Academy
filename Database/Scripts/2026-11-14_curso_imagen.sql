/* ============================================================================
   Cursos: imagen de portada (tabla CURSO_IMAGEN)
   Fecha: 2026-11-14
   - Idempotente: se puede ejecutar más de una vez sin efectos adicionales.
   - Solo CREA la tabla y su índice; no modifica CURSO ni datos existentes.
   - Igual que PRODUCTO_IMAGEN: Imagen_URL guarda el nombre del blob en el
     contenedor público de imágenes (p. ej. cursos/15/3f2a….webp), no la URL.
   - Hoy la Web usa una sola portada por curso (Es_Principal = 1); la tabla
     admite más imágenes por si luego se quiere una galería.
   ========================================================================= */

IF OBJECT_ID('dbo.CURSO_IMAGEN', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CURSO_IMAGEN
    (
        ID              INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_CURSO_IMAGEN PRIMARY KEY,
        FK_Curso        INT NOT NULL
            CONSTRAINT FK_CURSO_IMAGEN_CURSO REFERENCES dbo.CURSO(ID),
        Imagen_URL      VARCHAR(400) NOT NULL,
        Nombre_Archivo  NVARCHAR(255) NULL,
        Es_Principal    BIT NOT NULL
            CONSTRAINT DF_CURSO_IMAGEN_Es_Principal DEFAULT (0),
        Estado          BIT NOT NULL
            CONSTRAINT DF_CURSO_IMAGEN_Estado DEFAULT (1),
        Fecha_Creacion  DATETIME2(0) NOT NULL
            CONSTRAINT DF_CURSO_IMAGEN_Fecha_Creacion DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- Las imágenes siempre se consultan por curso.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_CURSO_IMAGEN_FK_Curso' AND object_id = OBJECT_ID('dbo.CURSO_IMAGEN'))
    CREATE INDEX IX_CURSO_IMAGEN_FK_Curso
        ON dbo.CURSO_IMAGEN (FK_Curso)
        INCLUDE (Estado, Es_Principal, Imagen_URL);
GO
