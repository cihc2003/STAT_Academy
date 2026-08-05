using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    [Table("TAREA")]
    public class TareaModel
    {
        [Key]
        public int ID { get; set; }

        [Column("FK_Curso")]
        public int CursoId { get; set; }

        public string Titulo { get; set; }

        public string Descripcion { get; set; }

        public DateTime Fecha_Inicio { get; set; }

        public DateTime Fecha_Limite { get; set; }

        public bool Entregada { get; set; }

        public bool Estado { get; set; }
    }
}