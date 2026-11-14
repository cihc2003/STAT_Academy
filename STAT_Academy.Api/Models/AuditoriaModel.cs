using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.Models
{
    public class AuditoriaModel
    {
        [Key]
        public int id { get; set; }

        public string? entidad { get; set; }
        public string? accion { get; set; }
        public string? descripcion { get; set; }
        public string? usuario { get; set; }

        public int? entidad_id { get; set; }

        public DateTime fecha { get; set; }
    }
}