/* ============================================================================
   Productos: reglas en la base de datos e índice de imágenes
   Fecha: 2026-11-10
   - Idempotente: se puede ejecutar más de una vez.
   - Solo AGREGA restricciones e índices. Si algún registro existente las
     incumpliera, el script falla en esa restricción sin cambiar nada (WITH CHECK).
   - La API ya valida lo mismo; esto evita datos inválidos que entren por otra vía.
   ========================================================================= */

-- El precio no puede ser negativo.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PRODUCTO_Precio_Base')
    ALTER TABLE dbo.PRODUCTO WITH CHECK
        ADD CONSTRAINT CK_PRODUCTO_Precio_Base CHECK (Precio_Base IS NULL OR Precio_Base >= 0);
GO

-- Las existencias no pueden ser negativas (la compra descuenta solo si alcanza).
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PRODUCTO_Stock')
    ALTER TABLE dbo.PRODUCTO WITH CHECK
        ADD CONSTRAINT CK_PRODUCTO_Stock CHECK (Stock IS NULL OR Stock >= 0);
GO

-- El stock mínimo no puede ser negativo.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PRODUCTO_Min_Stock')
    ALTER TABLE dbo.PRODUCTO WITH CHECK
        ADD CONSTRAINT CK_PRODUCTO_Min_Stock CHECK (Min_Stock IS NULL OR Min_Stock >= 0);
GO

-- Una línea de factura siempre tiene al menos una unidad y montos no negativos.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DETALLE_FACTURA_Valores')
    ALTER TABLE dbo.DETALLE_FACTURA WITH CHECK
        ADD CONSTRAINT CK_DETALLE_FACTURA_Valores CHECK (
            (Cantidad IS NULL OR Cantidad > 0)
            AND (Precio_Unitario IS NULL OR Precio_Unitario >= 0)
            AND (Subtotal IS NULL OR Subtotal >= 0));
GO

-- Las imágenes siempre se consultan por producto.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_PRODUCTO_IMAGEN_FK_Producto' AND object_id = OBJECT_ID('dbo.PRODUCTO_IMAGEN'))
    CREATE INDEX IX_PRODUCTO_IMAGEN_FK_Producto
        ON dbo.PRODUCTO_IMAGEN (FK_Producto)
        INCLUDE (Estado, Es_Principal);
GO
