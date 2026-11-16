using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.DTOs.Carrito;

namespace STAT_Academy.Api.Services
{
    public class CarritoService
    {
        private readonly ApplicationDbContext _context;

        public CarritoService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Cada usuario tiene un único carrito; si no existe todavía, se crea aquí.
        private async Task<CarritoModel> ObtenerOCrearCarritoAsync(int usuarioId)
        {
            var carrito = await _context.Carrito.FirstOrDefaultAsync(c => c.fk_Usuario == usuarioId);

            if (carrito == null)
            {
                carrito = new CarritoModel { fk_Usuario = usuarioId };
                _context.Carrito.Add(carrito);
                await _context.SaveChangesAsync();
            }

            return carrito;
        }

        public async Task<CarritoResponse> GetCarritoAsync(int usuarioId)
        {
            var carrito = await ObtenerOCrearCarritoAsync(usuarioId);

            var items = await (
                from detalle in _context.CarritoDetalle
                where detalle.fk_Carrito == carrito.id && detalle.fk_Producto != null
                join producto in _context.Producto on detalle.fk_Producto equals producto.id
                select new CarritoItemResponse
                {
                    detalleId = detalle.id,
                    productoId = producto.id,
                    nombre = producto.nombre,
                    precio_unitario = producto.precio_base,
                    cantidad = detalle.cantidad,
                    stock_disponible = producto.stock
                }
            ).ToListAsync();

            return new CarritoResponse
            {
                carritoId = carrito.id,
                items = items
            };
        }

        public async Task<CarritoResponse> AgregarItemAsync(AgregarAlCarritoRequest request)
        {
            var producto = await _context.Producto.FirstOrDefaultAsync(p => p.id == request.productoId && p.estado);

            if (producto == null)
                throw new InvalidOperationException("Producto no existe o no está disponible.");

            var carrito = await ObtenerOCrearCarritoAsync(request.usuarioId);

            var detalle = await _context.CarritoDetalle.FirstOrDefaultAsync(d =>
                d.fk_Carrito == carrito.id && d.fk_Producto == request.productoId);

            var cantidadDeseada = (detalle?.cantidad ?? 0) + request.cantidad;

            if (cantidadDeseada > producto.stock)
                throw new InvalidOperationException($"Solo hay {producto.stock} unidades disponibles.");

            if (detalle == null)
            {
                detalle = new CarritoDetalleModel
                {
                    fk_Carrito = carrito.id,
                    fk_Producto = request.productoId,
                    cantidad = request.cantidad
                };
                _context.CarritoDetalle.Add(detalle);
            }
            else
            {
                detalle.cantidad = cantidadDeseada;
            }

            await _context.SaveChangesAsync();

            return await GetCarritoAsync(request.usuarioId);
        }

        public async Task<CarritoResponse> ActualizarCantidadAsync(int detalleId, int usuarioId, int cantidad)
        {
            var carrito = await ObtenerOCrearCarritoAsync(usuarioId);

            var detalle = await _context.CarritoDetalle.FirstOrDefaultAsync(d =>
                d.id == detalleId && d.fk_Carrito == carrito.id);

            if (detalle == null)
                throw new InvalidOperationException("El producto no está en el carrito.");

            var producto = await _context.Producto.FirstOrDefaultAsync(p => p.id == detalle.fk_Producto);

            if (producto != null && cantidad > producto.stock)
                throw new InvalidOperationException($"Solo hay {producto.stock} unidades disponibles.");

            detalle.cantidad = cantidad;
            await _context.SaveChangesAsync();

            return await GetCarritoAsync(usuarioId);
        }

        public async Task<CarritoResponse> EliminarItemAsync(int detalleId, int usuarioId)
        {
            var carrito = await ObtenerOCrearCarritoAsync(usuarioId);

            var detalle = await _context.CarritoDetalle.FirstOrDefaultAsync(d =>
                d.id == detalleId && d.fk_Carrito == carrito.id);

            if (detalle != null)
            {
                _context.CarritoDetalle.Remove(detalle);
                await _context.SaveChangesAsync();
            }

            return await GetCarritoAsync(usuarioId);
        }
    }
}