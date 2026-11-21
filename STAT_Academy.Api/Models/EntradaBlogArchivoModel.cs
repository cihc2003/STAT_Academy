using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>Imagen o documento adjunto a una entrada del blog (dbo.ENTRADA_BLOG_ARCHIVO).</summary>
    [Table("ENTRADA_BLOG_ARCHIVO")]
    public class EntradaBlogArchivoModel
    {
        [Key]
        [Column("ID")]
        public int id { get; set; }

        [Column("FK_Entrada_Blog")]
        public int fk_Entrada_Blog { get; set; }

        [Column("Nombre_Archivo")]
        public string? nombre_Archivo { get; set; }

        [Column("Archivo_URL")]
        public string archivo_URL { get; set; } = "";

        /// <summary>Tipo MIME validado por la API (no el que declara el cliente).</summary>
        [Column("Tipo_Archivo")]
        public string? tipo_Archivo { get; set; }

        [Column("Es_Principal")]
        public bool es_Principal { get; set; }

        [Column("Estado")]
        public bool estado { get; set; } = true;

        [NotMapped]
        public bool EsImagen => tipo_Archivo?.StartsWith("image/") == true;
    }
}
