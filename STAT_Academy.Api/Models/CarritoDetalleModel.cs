using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.Models
{
    public class CarritoDetalleModel
    {
        [Key]
        public int id { get; set; }

        public int fk_Carrito { get; set; }

        // Solo trabajamos con productos por ahora; fk_Curso queda listo para cuando se conecte esa parte.
        public int? fk_Producto { get; set; }
        public int? fk_Curso { get; set; }

        public int cantidad { get; set; }
    }
}