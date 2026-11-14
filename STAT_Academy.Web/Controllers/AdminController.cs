using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Helpers;
using STAT_Academy.Web.Models.Admin;
using STAT_Academy.Web.Models.Facturas;
using STAT_Academy.Web.Models.Productos;
using STAT_Academy.Web.Services;
using System.Security.Claims;

namespace STAT_Academy.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApiUsuarioService _apiUsuarios;
        private readonly ApiProductoService _apiProductos;
        private readonly ApiProveedorService _apiProveedores;
        private readonly ApiCursoService _apiCursos;
        private readonly ApiFacturaService _apiFacturas;

        public AdminController(
            ApiUsuarioService apiUsuarios,
            ApiProductoService apiProductos,
            ApiProveedorService apiProveedores,
            ApiCursoService apiCursos,
            ApiFacturaService apiFacturas)
        {
            _apiUsuarios = apiUsuarios;
            _apiProductos = apiProductos;
            _apiProveedores = apiProveedores;
            _apiCursos = apiCursos;
            _apiFacturas = apiFacturas;
        }

        private int UsuarioId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Dashboard()
        {
            var usuarios = await _apiUsuarios.GetUsuariosAsync();
            var cursos = await _apiCursos.GetCursosAsync();

            ViewBag.Users = usuarios?.Count ?? 0;
            var productos = await _apiProductos.GetProductos() ?? [];
            ViewBag.Products = productos.Count;
            ViewBag.StockBajo = await _apiProductos.GetStockBajo();
            ViewBag.Courses = cursos?.Count ?? 0;
            ViewBag.Enrollments = 0;
            ViewBag.Orders = 0;
            ViewBag.PagosPendientes = await _apiFacturas.PagosPendientes();

            return View();
        }

        public async Task<IActionResult> Courses(string? search)
        {
            var cursos = await _apiCursos.GetCursosAsync() ?? [];

            var rows = cursos.Select(c => new CursoAdminResponse
            {
                id = c.id,
                fk_tutor = c.fk_tutor,
                fk_creador = c.fk_creador,
                nombre = c.nombre,
                descripcion = c.descripcion,
                precio = c.precio,
                duracionSemanas = c.duracionSemanas,
                estado = c.estado,
                fechaInicio = c.fechaInicio,
                fechaFin = c.fechaFin
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                rows = rows.Where(c =>
                    c.nombre.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.descripcion.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return View(rows);
        }

        /// <summary>
        /// Lista de tutores activos para el selector. Si el curso tiene asignado a alguien que
        /// ya no es un tutor válido, se muestra marcado para que el administrador lo corrija.
        /// </summary>
        private async Task CargarTutores(int? tutorActual)
        {
            var tutores = (await _apiUsuarios.FiltrarPorTipoAsync(2) ?? [])
                .Where(u => u.activo)
                .OrderBy(u => u.nombre)
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.id.ToString(),
                    Text = string.IsNullOrWhiteSpace(u.nombre) ? u.email : $"{u.nombre} ({u.email})",
                    Selected = u.id == tutorActual
                })
                .ToList();

            if (tutorActual is > 0 && tutores.All(t => t.Value != tutorActual.ToString()))
            {
                tutores.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = tutorActual.ToString(),
                    Text = $"⚠ Usuario #{tutorActual}: no es un tutor activo, elija otro",
                    Selected = true
                });
            }

            ViewBag.Tutores = tutores;
        }

        public async Task<IActionResult> CourseForm(int? id)
        {
            if (id == null)
            {
                await CargarTutores(null);

                return View(new CursoFormViewModel
                {
                    estado = true
                });
            }

            var curso = await _apiCursos.GetCursoByIdAsync(id.Value);

            if (curso == null)
                return NotFound();

            var model = new CursoFormViewModel
            {
                id = curso.id,
                fk_tutor = curso.fk_tutor,
                fk_creador = curso.fk_creador,
                nombre = curso.nombre,
                descripcion = curso.descripcion,
                precio = curso.precio,
                duracionSemanas = curso.duracionSemanas,
                estado = curso.estado,
                fechaInicio = curso.fechaInicio,
                fechaFin = curso.fechaFin
            };

            await CargarTutores(model.fk_tutor);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseForm(CursoFormViewModel model)
        {
            if (model.fk_tutor <= 0)
                ModelState.AddModelError(nameof(model.fk_tutor), "Seleccione el tutor del curso.");

            if (!ModelState.IsValid)
            {
                await CargarTutores(model.fk_tutor);
                return View(model);
            }

            model.fk_creador = UsuarioId;

            var resultado = model.id == null
                ? await _apiCursos.CrearCursoAsync(model)
                : await _apiCursos.EditarCursoAsync(model.id.Value, model);

            if (!resultado.ok)
            {
                // Mensaje real de la API (p. ej. "El usuario 3 no tiene rol de tutor.").
                ModelState.AddModelError("", resultado.message);
                await CargarTutores(model.fk_tutor);
                return View(model);
            }

            TempData["Success"] = model.id == null
                ? "Curso creado correctamente."
                : "Curso actualizado correctamente.";

            return RedirectToAction(nameof(Courses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourse(int id)
        {
            var curso = await _apiCursos.GetCursoByIdAsync(id);

            if (curso == null)
            {
                TempData["Error"] = "No se pudo encontrar el curso.";
                return RedirectToAction(nameof(Courses));
            }

            var resultado = curso.estado
                ? await _apiCursos.DesactivarCursoAsync(id)
                : await _apiCursos.ActivarCursoAsync(id);

            if (!resultado.ok)
            {
                TempData["Error"] = resultado.message;
            }
            else
            {
                TempData["Success"] = curso.estado
                    ? "Curso desactivado correctamente."
                    : "Curso activado correctamente.";
            }

            return RedirectToAction(nameof(Courses));
        }
    

     public async Task<IActionResult> Products(string? search, bool stockBajo = false)
        {
            var productosApi = await _apiProductos.GetProductos() ?? [];

            var rows = productosApi.Select(p => new ProductoViewModel
            {
                id = p.id,
                nombre = p.nombre,
                categoria = p.categoria,
                descripcion = p.descripcion,
                precio_base = p.precio_base,
                stock = p.stock,
                min_stock = p.min_stock,
                estado = p.estado,
                fk_proveedor = p.fk_proveedor,
                stock_bajo = p.stock_bajo,
                imagen_principal = p.imagen_principal
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                rows = rows.Where(p =>
                    p.nombre.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.categoria.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.TotalStockBajo = rows.Count(p => p.estado && p.stock_bajo);
            ViewBag.FiltroStockBajo = stockBajo;

            if (stockBajo)
                rows = rows.Where(p => p.estado && p.stock_bajo).ToList();

            // Lo que hay que reponer primero.
            rows = rows.OrderByDescending(p => p.estado && p.stock_bajo).ThenBy(p => p.nombre).ToList();

            return View(rows);
        }

        public async Task<IActionResult> ProductForm(int? id)
        {
            var proveedores = await _apiProductos.GetProveedores() ?? [];

            if (id == null)
            {
                return View(new ProductoFormViewModel
                {
                    ProveedoresDisponibles = proveedores
                });
            }

            var producto = await _apiProductos.GetProductoById(id.Value);

            if (producto == null)
            {
                return NotFound();
            }

            var model = new ProductoFormViewModel
            {
                id = producto.id,
                nombre = producto.nombre,
                categoria = producto.categoria,
                descripcion = producto.descripcion,
                precio_base = producto.precio_base,
                stock = producto.stock,
                min_stock = producto.min_stock,
                fk_proveedor = producto.fk_proveedor,
                ProveedoresDisponibles = proveedores,
                Imagenes = producto.imagenes
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(ProductoFormViewModel.MaxBytesPeticion)]
        [RequestFormLimits(MultipartBodyLengthLimit = ProductoFormViewModel.MaxBytesPeticion)]
        public async Task<IActionResult> ProductForm(ProductoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return await VolverAlFormulario(model);
            }

            var request = new ProductoCreateRequest
            {
                nombre = model.nombre,
                categoria = model.categoria,
                descripcion = model.descripcion,
                precio_base = model.precio_base,
                stock = model.stock,
                min_stock = model.min_stock,
                fk_proveedor = model.fk_proveedor
            };

            var (resultado, mensaje) = model.id == null
                ? await _apiProductos.CrearProducto(request)
                : await _apiProductos.EditarProducto(model.id.Value, request);

            if (resultado == null)
            {
                // Mensaje real de la API (p. ej. "El precio debe ser mayor que 0").
                ModelState.AddModelError("", mensaje);
                return await VolverAlFormulario(model);
            }

            // Al crear, las imágenes elegidas se suben ahora que el producto ya existe.
            if (model.id == null && model.NuevasImagenes.Count > 0)
            {
                var (exitoso, mensajeImagenes) = await _apiProductos.SubirImagenes(resultado.id, model.NuevasImagenes);
                if (!exitoso)
                {
                    TempData["Error"] = "El producto se creó, pero las imágenes no: " + mensajeImagenes;
                    return RedirectToAction(nameof(ProductForm), new { id = resultado.id });
                }
            }

            TempData["Success"] = model.id == null
                ? "Producto creado correctamente."
                : "Producto actualizado correctamente.";

            if (resultado.stock_bajo && resultado.estado)
                TempData["Error"] = $"Atención: \"{resultado.nombre}\" quedó con {resultado.stock} unidades, en o por debajo del mínimo ({resultado.min_stock}).";

            return RedirectToAction(nameof(Products));
        }

        private async Task<IActionResult> VolverAlFormulario(ProductoFormViewModel model)
        {
            model.ProveedoresDisponibles = await _apiProductos.GetProveedores() ?? [];
            if (model.id != null)
                model.Imagenes = (await _apiProductos.GetProductoById(model.id.Value))?.imagenes ?? [];
            return View(nameof(ProductForm), model);
        }

        // --- Imágenes del producto ----------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(ProductoFormViewModel.MaxBytesPeticion)]
        [RequestFormLimits(MultipartBodyLengthLimit = ProductoFormViewModel.MaxBytesPeticion)]
        public async Task<IActionResult> SubirImagenesProducto(int id, List<IFormFile> imagenes)
        {
            if (imagenes.Count == 0)
            {
                TempData["Error"] = "Seleccione al menos una imagen.";
            }
            else
            {
                var (exitoso, mensaje) = await _apiProductos.SubirImagenes(id, imagenes);
                TempData[exitoso ? "Success" : "Error"] = mensaje;
            }

            return RedirectToAction(nameof(ProductForm), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImagenPrincipalProducto(int id, int imagenId)
        {
            var (exitoso, mensaje) = await _apiProductos.MarcarPrincipal(id, imagenId);
            TempData[exitoso ? "Success" : "Error"] = mensaje;
            return RedirectToAction(nameof(ProductForm), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarImagenProducto(int id, int imagenId)
        {
            var (exitoso, mensaje) = await _apiProductos.QuitarImagen(id, imagenId);
            TempData[exitoso ? "Success" : "Error"] = mensaje;
            return RedirectToAction(nameof(ProductForm), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProduct(int id)
        {
            var producto = await _apiProductos.DesactivarProducto(id);

            if (producto == null)
            {
                TempData["Error"] = "No se pudo eliminar el producto.";
            }
            else
            {
                TempData["Success"] = "Producto eliminado de la tienda correctamente.";
            }

            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProductOn(int id)
        {
            var producto = await _apiProductos.ActivarProducto(id);

            if (producto == null)
            {
                TempData["Error"] = "No se pudo reactivar el producto.";
            }
            else
            {
                TempData["Success"] = "Producto reactivado correctamente.";
            }

            return RedirectToAction(nameof(Products));
        }

        // ---------------------------------------------------------------
        // Facturas y pagos
        // ---------------------------------------------------------------

        /// <param name="vista">"pagos" (comprobantes, por defecto) o "facturas".</param>
        /// <param name="estado">Comprobantes: PENDIENTE (por defecto), APROBADO, RECHAZADO o TODOS. Facturas: estado de la factura.</param>
        /// <param name="buscar">Nombre o correo del usuario, o número de compra.</param>
        public async Task<IActionResult> Invoices(string? vista, string? estado, string? buscar)
        {
            var model = new FacturasAdminViewModel
            {
                Vista = vista == "facturas" ? "facturas" : "pagos",
                Buscar = buscar?.Trim(),
                Pendientes = await _apiFacturas.PagosPendientes()
            };

            bool Coincide(int facturaId, string nombre, string correo) =>
                string.IsNullOrWhiteSpace(model.Buscar)
                || facturaId.ToString() == model.Buscar.TrimStart('#')
                || Busqueda.Coincide(nombre, model.Buscar)
                || Busqueda.Coincide(correo, model.Buscar);

            if (model.Vista == "pagos")
            {
                model.Estado = string.IsNullOrWhiteSpace(estado) ? "PENDIENTE" : estado.ToUpperInvariant();
                var pagos = await _apiFacturas.Pagos(model.Estado == "TODOS" ? null : model.Estado) ?? [];
                var filtrados = pagos.Where(p => Coincide(p.facturaId ?? 0, p.usuarioNombre, p.usuarioEmail));

                // Lo pendiente, por orden de llegada (el más antiguo primero); lo revisado, lo último primero.
                model.Pagos = filtrados.Where(p => p.estado == "PENDIENTE").OrderBy(p => p.fechaEnvio)
                    .Concat(filtrados.Where(p => p.estado != "PENDIENTE").OrderByDescending(p => p.fechaRevision ?? p.fechaEnvio))
                    .ToList();
            }
            else
            {
                model.Estado = string.IsNullOrWhiteSpace(estado) ? null : estado.ToUpperInvariant();
                var facturas = await _apiFacturas.TodasLasFacturas() ?? [];
                model.Facturas = facturas
                    .Where(f => model.Estado == null || f.estado == model.Estado)
                    .Where(f => Coincide(f.id, f.usuarioNombre, f.usuarioEmail))
                    .ToList();
            }

            return View(model);
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var factura = await _apiFacturas.Detalle(id);
            return factura == null ? NotFound() : View(factura);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePayment(int pagoId, int facturaId)
        {
            var (exitoso, mensaje) = await _apiFacturas.Aprobar(pagoId);
            TempData[exitoso ? "Success" : "Error"] = mensaje;
            return RedirectToAction(nameof(Invoice), new { id = facturaId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(int pagoId, int facturaId, string? observacion)
        {
            if (string.IsNullOrWhiteSpace(observacion))
            {
                TempData["Error"] = "Indica el motivo del rechazo: se le enviará al usuario.";
                return RedirectToAction(nameof(Invoice), new { id = facturaId });
            }

            var (exitoso, mensaje) = await _apiFacturas.Rechazar(pagoId, observacion);
            TempData[exitoso ? "Success" : "Error"] = mensaje;
            return RedirectToAction(nameof(Invoice), new { id = facturaId });
        }
    }
}