using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>
    /// Pago confirmado de una compra (dbo.PAGO). Se crea cuando un administrador aprueba
    /// el comprobante (PEDIDO_PAGO) y queda enlazado desde FACTURA.FK_Pago.
    /// </summary>
    [Table("PAGO")]
    public class PagoModel
    {
        [Key, Column("ID")] public int id { get; set; }
        [Column("FK_Usuario")] public int? fk_Usuario { get; set; }
        [Column("FK_Metodo_Pago")] public int? fk_Metodo_Pago { get; set; }
        [Column("Monto")] public decimal? monto { get; set; }
        [Column("Fecha_Pago")] public DateTime? fecha_Pago { get; set; }
        [Column("Estado")] public bool? estado { get; set; }
    }

    /// <summary>Factura de una compra (dbo.FACTURA).</summary>
    [Table("FACTURA")]
    public class FacturaModel
    {
        [Key, Column("ID")] public int id { get; set; }
        [Column("FK_Usuario")] public int? fk_Usuario { get; set; }
        [Column("FK_Pago")] public int? fk_Pago { get; set; }
        [Column("Fecha")] public DateTime? fecha { get; set; }
        [Column("Total")] public decimal? total { get; set; }
        [Column("Pago")] public bool? pagada { get; set; }

        public List<DetalleFacturaModel> Detalles { get; set; } = new();
    }

    /// <summary>Línea de factura (dbo.DETALLE_FACTURA). Guarda el precio del momento de la compra.</summary>
    [Table("DETALLE_FACTURA")]
    public class DetalleFacturaModel
    {
        [Key, Column("ID")] public int id { get; set; }
        [Column("FK_Factura")] public int? fk_Factura { get; set; }
        [Column("FK_Producto")] public int? fk_Producto { get; set; }
        [Column("FK_Curso")] public int? fk_Curso { get; set; }
        [Column("Cantidad")] public int? cantidad { get; set; }
        [Column("Precio_Unitario")] public decimal? precio_Unitario { get; set; }
        [Column("Subtotal")] public decimal? subtotal { get; set; }
    }

    /// <summary>
    /// Comprobante de pago enviado por el usuario (dbo.PEDIDO_PAGO): SINPE Móvil o transferencia.
    /// Cada envío es una fila; un administrador la aprueba o la rechaza.
    /// FK_Factura la agrega el script 2026-11-14_facturas_pagos.sql.
    /// </summary>
    [Table("PEDIDO_PAGO")]
    public class PedidoPagoModel
    {
        public const string Pendiente = "PENDIENTE";
        public const string Aprobado = "APROBADO";
        public const string Rechazado = "RECHAZADO";

        public const string MetodoSinpe = "SINPE";
        public const string MetodoTransferencia = "TRANSFERENCIA";

        [Key, Column("ID")] public int id { get; set; }
        [Column("FK_Usuario")] public int fk_Usuario { get; set; }
        [Column("FK_Factura")] public int? fk_Factura { get; set; }
        [Column("Metodo")] public string metodo { get; set; } = MetodoSinpe;
        [Column("Estado")] public string estado { get; set; } = Pendiente;
        [Column("Monto")] public decimal monto { get; set; }
        [Column("ComprobanteBlob")] public string comprobanteBlob { get; set; } = "";
        [Column("Referencia")] public string? referencia { get; set; }
        [Column("FechaCreacion")] public DateTime fechaCreacion { get; set; }
        [Column("FechaRevision")] public DateTime? fechaRevision { get; set; }
        [Column("FK_Admin")] public int? fk_Admin { get; set; }
        [Column("Observacion")] public string? observacion { get; set; }
    }

    /// <summary>Catálogo de métodos de pago (dbo.METODO_PAGO).</summary>
    [Table("METODO_PAGO")]
    public class MetodoPagoModel
    {
        [Key, Column("ID")] public int id { get; set; }
        [Column("Nombre")] public string? nombre { get; set; }
        [Column("Estado")] public bool? estado { get; set; }
    }
}
