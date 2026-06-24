using Microsoft.AspNetCore.Mvc;

namespace STAT_Academy.Api.DTOs.Proveedor
{
    public class ProveedorCreateRequest
    {
        public string nombre { get; set; }
        public string contacto { get; set; }
        public string telefono { get; set; }
        public string email { get; set; }
    }
}
