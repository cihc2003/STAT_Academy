using STAT_Academy.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace STAT_Academy.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<UsuarioModel> Usuario { get; set; }
        public DbSet<ProveedorModel> Proveedor { get; set; }
        public DbSet<AuditoriaModel> Auditoria { get; set; }
        public DbSet<ProductoModel> Producto { get; set; }
        public DbSet<CarritoModel> Carrito { get; set; }
        public DbSet<EstudianteCursoModel> EstudianteCurso { get; set; }
        public DbSet<CursoModel> Curso { get; set; }
        public DbSet<CarritoDetalleModel> CarritoDetalle { get; set; }
        public DbSet<TareaModel> Tarea { get; set; }
        public DbSet<EntradaBlogModel> EntradaBlog { get; set; }
        public DbSet<MaterialCursoModel> MaterialCurso { get; set; }
        public DbSet<EntradaBlogArchivoModel> EntradaBlogArchivo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioModel>()
                .HasIndex(u => u.email)
                .IsUnique();

            modelBuilder.Entity<CarritoDetalleModel>()
               .ToTable("CARRITO_DETALLE");
            modelBuilder.Entity<CursoModel>()
                .ToTable("CURSO");

            // Precisión según el esquema (Precio_Base decimal(10,2) en CURSO y PRODUCTO).
            modelBuilder.Entity<CursoModel>()
                .Property(c => c.precio)
                .HasPrecision(10, 2);

            modelBuilder.Entity<ProductoModel>()
                .Property(p => p.precio_base)
                .HasPrecision(10, 2);

            
            modelBuilder.Entity<EntradaBlogModel>()
             .ToTable("ENTRADA_BLOG");


            modelBuilder.Entity<EntradaBlogModel>()
                .HasOne(b => b.Autor)
                .WithMany()
                .HasForeignKey(b => b.fk_Autor)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EntradaBlogModel>()
                .HasMany(b => b.Archivos)
                .WithOne()
                .HasForeignKey(a => a.fk_Entrada_Blog)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

