using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>Imagen de un producto (dbo.PRODUCTO_IMAGEN), en el contenedor público "imagenes".</summary>
    [Table("PRODUCTO_IMAGEN")]
    public class ProductoImagenModel
    {
        [Key]
        public int id { get; set; }

        public int fk_producto { get; set; }

        /// <summary>Nombre del blob: productos/15/3f2a….webp</summary>
        public string imagen_url { get; set; } = "";

        public string? nombre_archivo { get; set; }

        public bool es_principal { get; set; }

        public bool estado { get; set; } = true;
    }
}
