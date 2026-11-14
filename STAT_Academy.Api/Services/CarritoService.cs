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
        private CarritoModel ObtenerOCrearCarrito(int usuarioId)
        {
            var carrito = _context.Carrito.FirstOrDefault(c => c.fk_Usuario == usuarioId);

            if (carrito == null)
            {
                carrito = new CarritoModel { fk_Usuario = usuarioId };
                _context.Carrito.Add(carrito);
                _context.SaveChanges();
            }

            return carrito;
        }

        public CarritoResponse GetCarrito(int usuarioId)
        {
            var carrito = ObtenerOCrearCarrito(usuarioId);

            var items = (
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
            ).ToList();

            return new CarritoResponse
            {
                carritoId = carrito.id,
                items = items
            };
        }

        public CarritoResponse AgregarItem(AgregarAlCarritoRequest request)
        {
            var producto = _context.Producto.FirstOrDefault(p => p.id == request.productoId && p.estado);

            if (producto == null)
                throw new Exception("Producto no existe o no está disponible.");

            var carrito = ObtenerOCrearCarrito(request.usuarioId);

            var detalle = _context.CarritoDetalle.FirstOrDefault(d =>
                d.fk_Carrito == carrito.id && d.fk_Producto == request.productoId);

            var cantidadDeseada = (detalle?.cantidad ?? 0) + request.cantidad;

            if (cantidadDeseada > producto.stock)
                throw new Exception($"Solo hay {producto.stock} unidades disponibles.");

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

            _context.SaveChanges();

            return GetCarrito(request.usuarioId);
        }

        public CarritoResponse ActualizarCantidad(int detalleId, int usuarioId, int cantidad)
        {
            var carrito = ObtenerOCrearCarrito(usuarioId);

            var detalle = _context.CarritoDetalle.FirstOrDefault(d =>
                d.id == detalleId && d.fk_Carrito == carrito.id);

            if (detalle == null)
                throw new Exception("El producto no está en el carrito.");

            var producto = _context.Producto.FirstOrDefault(p => p.id == detalle.fk_Producto);

            if (producto != null && cantidad > producto.stock)
                throw new Exception($"Solo hay {producto.stock} unidades disponibles.");

            detalle.cantidad = cantidad;
            _context.SaveChanges();

            return GetCarrito(usuarioId);
        }

        public CarritoResponse EliminarItem(int detalleId, int usuarioId)
        {
            var carrito = ObtenerOCrearCarrito(usuarioId);

            var detalle = _context.CarritoDetalle.FirstOrDefault(d =>
                d.id == detalleId && d.fk_Carrito == carrito.id);

            if (detalle != null)
            {
                _context.CarritoDetalle.Remove(detalle);
                _context.SaveChanges();
            }

            return GetCarrito(usuarioId);
        }
    }
}