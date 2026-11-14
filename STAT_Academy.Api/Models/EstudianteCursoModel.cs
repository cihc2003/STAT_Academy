using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    [Table("ESTUDIANTE_CURSO")]
    public class EstudianteCursoModel
    {
        [Key]
        [Column("ID_Matricula")]
        public int IdMatricula { get; set; }

        [Column("FK_Curso")]
        public int CursoId { get; set; }

        [Column("FK_Estudiante")]
        public int EstudianteId { get; set; }

        public DateTime Fecha_Matricula { get; set; }

        public string Estado { get; set; }

        public int Progreso { get; set; }
    }
}