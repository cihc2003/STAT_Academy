/* ============================================================================
   Facturas y pagos por SINPE Móvil / transferencia
   Fecha: 2026-11-14
   - Idempotente: se puede ejecutar más de una vez.
   - Reutiliza dbo.PEDIDO_PAGO como "pago con comprobante": cada comprobante que
     sube el usuario es una fila (PENDIENTE -> APROBADO / RECHAZADO).
   - Solo AGREGA una columna, una llave foránea e índices. No borra ni cambia datos.

   Relación resultante:
     FACTURA (la compra, Pago = 0 hasta que se aprueba)
       ├── DETALLE_FACTURA (productos o cursos)
       ├── PEDIDO_PAGO     (comprobantes enviados, FK_Factura)   <- nuevo vínculo
       └── PAGO            (pago confirmado, FACTURA.FK_Pago; se crea al aprobar)
   ========================================================================= */

-- Los índices filtrados exigen estas opciones (la base tiene ambas en OFF por defecto).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Vínculo del comprobante con la factura que paga.
IF COL_LENGTH('dbo.PEDIDO_PAGO', 'FK_Factura') IS NULL
    ALTER TABLE dbo.PEDIDO_PAGO ADD FK_Factura INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PEDIDO_PAGO_FACTURA')
    ALTER TABLE dbo.PEDIDO_PAGO WITH CHECK
        ADD CONSTRAINT FK_PEDIDO_PAGO_FACTURA FOREIGN KEY (FK_Factura) REFERENCES dbo.FACTURA (ID);
GO

-- Los pagos siempre se consultan por factura.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_PEDIDO_PAGO_FK_Factura' AND object_id = OBJECT_ID('dbo.PEDIDO_PAGO'))
    CREATE INDEX IX_PEDIDO_PAGO_FK_Factura ON dbo.PEDIDO_PAGO (FK_Factura) INCLUDE (Estado);
GO

-- Como máximo UN comprobante en verificación por factura (evita envíos dobles simultáneos).
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_PEDIDO_PAGO_Factura_Pendiente' AND object_id = OBJECT_ID('dbo.PEDIDO_PAGO'))
    CREATE UNIQUE INDEX UX_PEDIDO_PAGO_Factura_Pendiente
        ON dbo.PEDIDO_PAGO (FK_Factura)
        WHERE Estado = 'PENDIENTE' AND FK_Factura IS NOT NULL;
GO

-- Como máximo UN pago aprobado por factura (una factura no se cobra dos veces).
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_PEDIDO_PAGO_Factura_Aprobado' AND object_id = OBJECT_ID('dbo.PEDIDO_PAGO'))
    CREATE UNIQUE INDEX UX_PEDIDO_PAGO_Factura_Aprobado
        ON dbo.PEDIDO_PAGO (FK_Factura)
        WHERE Estado = 'APROBADO' AND FK_Factura IS NOT NULL;
GO

-- La revisión del administrador lista primero lo pendiente.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_PEDIDO_PAGO_Estado' AND object_id = OBJECT_ID('dbo.PEDIDO_PAGO'))
    CREATE INDEX IX_PEDIDO_PAGO_Estado ON dbo.PEDIDO_PAGO (Estado, FechaCreacion);
GO

-- "Mis facturas": siempre por usuario.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_FACTURA_FK_Usuario' AND object_id = OBJECT_ID('dbo.FACTURA'))
    CREATE INDEX IX_FACTURA_FK_Usuario ON dbo.FACTURA (FK_Usuario, Fecha);
GO
