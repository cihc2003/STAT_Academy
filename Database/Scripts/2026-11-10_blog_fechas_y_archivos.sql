/* ============================================================================
   Blog: fecha de creación/edición e índice de archivos adjuntos
   Fecha: 2026-11-10
   - Idempotente: se puede ejecutar más de una vez sin efectos adicionales.
   - Solo AGREGA columnas e índices; no borra ni modifica datos existentes.
   - Las entradas que ya existen reciben como Fecha_Creacion el momento en que
     se ejecuta este script (no hay forma de recuperar la fecha original).
   ========================================================================= */

-- Fecha en que se crea la entrada (UTC). La API la asigna al crear;
-- el DEFAULT cubre inserciones hechas fuera de la API.
IF COL_LENGTH('dbo.ENTRADA_BLOG', 'Fecha_Creacion') IS NULL
BEGIN
    ALTER TABLE dbo.ENTRADA_BLOG
        ADD Fecha_Creacion DATETIME2(0) NOT NULL
            CONSTRAINT DF_ENTRADA_BLOG_Fecha_Creacion DEFAULT (SYSUTCDATETIME())
            WITH VALUES;
END
GO

-- Última edición (UTC); NULL si nunca se editó.
IF COL_LENGTH('dbo.ENTRADA_BLOG', 'Fecha_Edicion') IS NULL
BEGIN
    ALTER TABLE dbo.ENTRADA_BLOG
        ADD Fecha_Edicion DATETIME2(0) NULL;
END
GO

-- Los archivos siempre se consultan por entrada: índice sobre la FK.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ENTRADA_BLOG_ARCHIVO_FK_Entrada_Blog'
      AND object_id = OBJECT_ID('dbo.ENTRADA_BLOG_ARCHIVO'))
BEGIN
    CREATE INDEX IX_ENTRADA_BLOG_ARCHIVO_FK_Entrada_Blog
        ON dbo.ENTRADA_BLOG_ARCHIVO (FK_Entrada_Blog)
        INCLUDE (Estado, Es_Principal);
END
GO
