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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioModel>()
                .HasIndex(u => u.email)
                .IsUnique();
        }
    }
}

