using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAT_Academy.Web.Services;
using System.Security.Claims;

namespace STAT_Academy.Web.Controllers;

[Authorize]
public class CarritoController : Controller
{
    private readonly ApiCarritoService _apiCarrito;

    public CarritoController(ApiCarritoService apiCarrito)
    {
        _apiCarrito = apiCarrito;
    }

    // El id del usuario logueado viaja en la cookie desde el Login (CuentaController).
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var carrito = await _apiCarrito.GetCarrito(UsuarioId);
        return View(carrito);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(int productoId, int cantidad)
    {
        var (exito, error, _) = await _apiCarrito.Agregar(UsuarioId, productoId, cantidad);

        if (!exito)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] = "Producto agregado al carrito exitosamente.";
        }

        return RedirectToAction("Details", "Products", new { id = productoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(int detalleId, int cantidad)
    {
        var (exito, error, _) = await _apiCarrito.ActualizarCantidad(detalleId, UsuarioId, cantidad);

        if (!exito)
        {
            TempData["Error"] = error;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int detalleId)
    {
        await _apiCarrito.Eliminar(detalleId, UsuarioId);
        TempData["Success"] = "Producto eliminado del carrito.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Checkout()
    {
        var carrito = await _apiCarrito.GetCarrito(UsuarioId);

        if (carrito == null || !carrito.items.Any())
        {
            TempData["Error"] = "Tu carrito está vacío.";
            return RedirectToAction(nameof(Index));
        }

        return View(carrito);
    }

    /// <summary>
    /// Confirma la compra: se crea la factura pendiente de pago, se reservan las unidades y
    /// se envía el comprobante por correo. Luego el usuario paga y sube el comprobante.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Confirmar()
    {
        var (compra, mensaje) = await _apiCarrito.Comprar();

        if (compra == null)
        {
            // P. ej. "Solo quedan 2 unidades de …": se vuelve al carrito para ajustar.
            TempData["Error"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "¡Compra registrada! Te enviamos el comprobante por correo. Realiza el pago y sube el comprobante aquí.";
        return RedirectToAction("Detalle", "Facturas", new { id = compra.facturaId });
    }

    /// <summary>Enlace anterior al comprobante: ahora vive en "Mis facturas y pagos".</summary>
    public IActionResult Compra(int id) => RedirectToAction("Detalle", "Facturas", new { id });
}