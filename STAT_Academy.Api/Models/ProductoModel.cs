using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    [Table("PRODUCTO")]
    public class ProductoModel
    {
        [Key]
        public int id { get; set; }

        public string nombre { get; set; } = "";
        public string categoria { get; set; } = "";
        public string descripcion { get; set; } = "";

        public decimal precio_base { get; set; }
        public int stock { get; set; }
        public int min_stock { get; set; }

        public bool estado { get; set; }

        public int fk_proveedor { get; set; }

        /// <summary>
        /// Copia del blob de la imagen principal (PRODUCTO.Imagen_URL), para listados rápidos.
        /// La fuente de verdad es PRODUCTO_IMAGEN.Es_Principal.
        /// </summary>
        [Column("Imagen_URL")]
        public string? imagen_url { get; set; }

        public List<ProductoImagenModel> Imagenes { get; set; } = new();

        /// <summary>Stock en o por debajo del mínimo: hay que reponer.</summary>
        [NotMapped]
        public bool StockBajo => stock <= min_stock;
    }
}
