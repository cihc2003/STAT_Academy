using System.ComponentModel.DataAnnotations;
using STAT_Academy.Web.Models.Facturas;

namespace STAT_Academy.Web.Models.Perfil
{
    /// <summary>Respuesta de GET api/Perfil.</summary>
    public class PerfilDatos
    {
        public string nombre { get; set; } = "";
        public string email { get; set; } = "";
        public string tipo { get; set; } = "";
        public DateTime? fecha_Registro { get; set; }
        public DateTime? ultimo_Login { get; set; }
    }

    /// <summary>Formulario "Mis datos": solo el nombre (el correo no se edita).</summary>
    public class EditarPerfilViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
        [Display(Name = "Nombre completo")]
        public string nombre { get; set; } = "";
    }

    public class PerfilViewModel
    {
        public PerfilDatos Datos { get; set; } = new();
        public EditarPerfilViewModel Formulario { get; set; } = new();
    }

    /// <summary>Respuesta de GET api/Factura/mias/buscar.</summary>
    public class FacturaPagina
    {
        public List<FacturaResumen> items { get; set; } = [];
        public int pagina { get; set; }
        public int tamano { get; set; }
        public int total { get; set; }
        public int totalPaginas { get; set; }
        public string orden { get; set; } = "recientes";
        public int porPagar { get; set; }
    }
}
