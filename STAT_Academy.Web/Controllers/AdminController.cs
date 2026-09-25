using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Models.Admin;
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

        public AdminController(
            ApiUsuarioService apiUsuarios,
            ApiProductoService apiProductos,
            ApiProveedorService apiProveedores,
            ApiCursoService apiCursos)
        {
            _apiUsuarios = apiUsuarios;
            _apiProductos = apiProductos;
            _apiProveedores = apiProveedores;
            _apiCursos = apiCursos;
        }

        private int UsuarioId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> Dashboard()
        {
            var usuarios = await _apiUsuarios.GetUsuariosAsync();
            var cursos = await _apiCursos.GetCursosAsync();

            ViewBag.Users = usuarios?.Count ?? 0;
            ViewBag.Products = 0;
            ViewBag.Courses = cursos?.Count ?? 0;
            ViewBag.Enrollments = 0;
            ViewBag.Orders = 0;

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

        public async Task<IActionResult> CourseForm(int? id)
        {
            if (id == null)
            {
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

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseForm(CursoFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.fk_creador = UsuarioId;

            var resultado = model.id == null
                ? await _apiCursos.CrearCursoAsync(model)
                : await _apiCursos.EditarCursoAsync(model.id.Value, model);

            if (!resultado.ok)
            {
                ModelState.AddModelError("", "No se pudo guardar el curso. Verifica los datos e intenta de nuevo.");
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
    

     public async Task<IActionResult> Products(string? search)
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
                fk_proveedor = p.fk_proveedor
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                rows = rows.Where(p =>
                    p.nombre.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.categoria.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

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
                ProveedoresDisponibles = proveedores
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductForm(ProductoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ProveedoresDisponibles = await _apiProductos.GetProveedores() ?? [];
                return View(model);
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

            var resultado = model.id == null
                ? await _apiProductos.CrearProducto(request)
                : await _apiProductos.EditarProducto(model.id.Value, request);

            if (resultado == null)
            {
                ModelState.AddModelError("", "No se pudo guardar el producto. Verifica los datos e intenta de nuevo.");
                model.ProveedoresDisponibles = await _apiProductos.GetProveedores() ?? [];
                return View(model);
            }

            TempData["Success"] = model.id == null
                ? "Producto creado correctamente."
                : "Producto actualizado correctamente.";

            return RedirectToAction(nameof(Products));
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
    }
}