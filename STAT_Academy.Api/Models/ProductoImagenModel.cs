using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.Models
{
    public class ProductoImagenModel
    {
        [Key]
        public int id { get; set; }

        public int fk_producto { get; set; }

        public string imagen_url { get; set; } = "";       // nombre del blob: productos/15/3f2a...webp
        public string? nombre_archivo { get; set; }        // nombre original: "camiseta azul.webp"

        public bool? es_principal { get; set; }            // bit NULL en la tabla
        public bool estado { get; set; } = true;
    }
}