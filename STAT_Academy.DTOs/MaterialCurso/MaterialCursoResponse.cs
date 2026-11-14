namespace STAT_Academy.DTOs.MaterialCurso
{
    public class MaterialCursoResponse
    {
        public int Id { get; set; }

        public string Titulo { get; set; } = "";

        /// <summary>URL del enlace. Vacío para archivos: se descargan con api/MaterialCurso/{id}/descargar.</summary>
        public string UbicacionMaterial { get; set; } = "";

        public bool EsEnlace { get; set; }

        public string Tipo { get; set; } = "";

        public int? Semana { get; set; }
    }
}
