/* ============================================================================
   Cursos: tipo y prerrequisitos (columnas nuevas en CURSO)
   Fecha: 2026-11-16
   - Idempotente: se puede ejecutar más de una vez sin efectos adicionales.
   - Solo AGREGA dos columnas que admiten NULL; los cursos existentes quedan
     igual (sin tipo ni prerrequisitos) y no se modifica ningún dato.
   - Tipo: texto libre corto (p. ej. "Virtual", "Taller", "Certificación").
   - Prerrequisitos: texto libre (conocimientos o cursos previos recomendados).
   - Los largos coinciden con las validaciones de la API (CursoCreateRequest).
   - IMPORTANTE: aplicar ANTES de publicar la versión de la API que lee estas
     columnas; sin ellas, las consultas a CURSO fallan con "Invalid column name".
   ========================================================================= */

IF COL_LENGTH('dbo.CURSO', 'Tipo') IS NULL
    ALTER TABLE dbo.CURSO ADD Tipo NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.CURSO', 'Prerrequisitos') IS NULL
    ALTER TABLE dbo.CURSO ADD Prerrequisitos NVARCHAR(500) NULL;
GO
