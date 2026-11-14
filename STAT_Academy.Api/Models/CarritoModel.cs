using System.ComponentModel.DataAnnotations;

namespace STAT_Academy.Api.Models
{
    public class CarritoModel
    {
        [Key]
        public int id { get; set; }

        public int fk_Usuario { get; set; }
    }
}