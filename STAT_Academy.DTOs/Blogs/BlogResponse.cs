namespace STAT_Academy.DTOs.Blogs
{
    public class BlogResponse
    {
        public int id { get; set; }

        public string titulo { get; set; } = "";

        /// <summary>HTML ya limpio (lista blanca) listo para mostrar.</summary>
        public string contenido { get; set; } = "";

        /// <summary>Texto plano de ~220 caracteres para tarjetas y vistas previas.</summary>
        public string resumen { get; set; } = "";

        public int minutos_lectura { get; set; }

        /// <summary>UTC.</summary>
        public DateTime fecha_creacion { get; set; }

        /// <summary>UTC; null si nunca se editó.</summary>
        public DateTime? fecha_edicion { get; set; }

        public bool estado { get; set; }

        public int fk_Autor { get; set; }

        public string autor { get; set; } = "";

        /// <summary>URL de la imagen principal (portada), si hay alguna.</summary>
        public string? imagen_principal { get; set; }

        /// <summary>Adjuntos activos: imágenes y documentos.</summary>
        public List<BlogArchivoResponse> archivos { get; set; } = new();
    }

    public class BlogArchivoResponse
    {
        public int id { get; set; }

        public string nombre { get; set; } = "";

        public string url { get; set; } = "";

        public string tipo { get; set; } = "";

        public bool es_imagen { get; set; }

        public bool es_principal { get; set; }
    }
}
