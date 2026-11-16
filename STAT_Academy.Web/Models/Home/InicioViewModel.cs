using STAT_Academy.Web.Models.Blog;
using STAT_Academy.Web.Models.Productos;

namespace STAT_Academy.Web.Models.Home
{
    /// <summary>
    /// Portada con datos reales de la API (cursos, productos y blog públicos).
    /// Si una fuente no responde, su sección no se muestra; nunca se rellenan cifras.
    /// </summary>
    public class InicioViewModel
    {
        /// <summary>Cursos activos con inscripción abierta, del próximo inicio al más lejano.</summary>
        public List<CursoModel> Cursos { get; set; } = [];

        /// <summary>Productos activos: primero los disponibles.</summary>
        public List<ProductoResponse> Productos { get; set; } = [];

        public List<(string nombre, int cantidad)> Categorias { get; set; } = [];

        /// <summary>Entradas publicadas, de la más reciente a la más antigua.</summary>
        public List<BlogResponse> Articulos { get; set; } = [];

        /// <summary>Tutores distintos que imparten los cursos abiertos.</summary>
        public int Instructores { get; set; }

        public bool CursosDisponibles { get; set; }
        public bool ProductosDisponibles { get; set; }
        public bool BlogDisponible { get; set; }

        public IEnumerable<CursoModel> ProximosInicios => Cursos.Take(4);
    }
}
