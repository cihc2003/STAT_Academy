using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STAT_Academy.Api.Models
{
    /// <summary>Pago de una compra (dbo.PAGO). El pago es simulado: no hay pasarela.</summary>
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
}
