using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.Models
{
    public class ProveedorModel
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string contacto { get; set; }
        public string telefono { get; set; }
        public string email { get; set; }

        public bool estado { get; set; }
        public DateTime fecha_creacion { get; set; }
        public DateTime? fecha_edicion { get; set; }
    }
}