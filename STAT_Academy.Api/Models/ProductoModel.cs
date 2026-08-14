using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.Models
{
    public class ProductoModel
    {
        [Key]
        public int id { get; set; }

        public string nombre { get; set; }
        public string categoria { get; set; }
        public string descripcion { get; set; }

        public decimal precio_base { get; set; }
        public int stock { get; set; }
        public int min_stock { get; set; }

        public DateTime fecha_creacion { get; set; }
        public DateTime fecha_edicion { get; set; }

        public bool estado { get; set; }

        public int fk_proveedor { get; set; }
    }
}
