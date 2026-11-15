/* ============================================================================
   Cursos: nota final ajustada por el tutor (tabla NOTA_FINAL)
   Fecha: 2026-11-15
   - Idempotente: se puede ejecutar más de una vez sin efectos adicionales.
   - Solo CREA la tabla y su índice; no modifica ESTUDIANTE_CURSO ni datos existentes.
   - La nota final se CALCULA en la API a partir de las tareas (calificadas +
     vencidas sin entregar, que cuentan 0). Esta tabla guarda solo el AJUSTE que
     hace el tutor: si hay una fila, su Nota reemplaza a la calculada.
   - Una fila como máximo por matrícula (índice único en FK_Matricula).
   - Mientras no se aplique, la API sigue funcionando: muestra la nota calculada
     y el panel del tutor indica que el ajuste no está disponible.
   ========================================================================= */

IF OBJECT_ID('dbo.NOTA_FINAL', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NOTA_FINAL
    (
        ID              INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_NOTA_FINAL PRIMARY KEY,
        FK_Matricula    INT NOT NULL
            CONSTRAINT FK_NOTA_FINAL_MATRICULA REFERENCES dbo.ESTUDIANTE_CURSO(ID_Matricula),
        Nota            DECIMAL(5,2) NOT NULL
            CONSTRAINT CK_NOTA_FINAL_Nota CHECK (Nota BETWEEN 0 AND 100),
        Comentario      NVARCHAR(255) NULL,
        FK_Tutor        INT NULL
            CONSTRAINT FK_NOTA_FINAL_TUTOR REFERENCES dbo.USUARIO(ID),
        Fecha_Edicion   DATETIME2(0) NOT NULL
            CONSTRAINT DF_NOTA_FINAL_Fecha_Edicion DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_NOTA_FINAL_FK_Matricula' AND object_id = OBJECT_ID('dbo.NOTA_FINAL'))
    CREATE UNIQUE INDEX UX_NOTA_FINAL_FK_Matricula
        ON dbo.NOTA_FINAL (FK_Matricula)
        INCLUDE (Nota);
GO
