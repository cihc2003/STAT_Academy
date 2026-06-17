using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Models;

namespace STAT_Academy.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<CourseMaterial> CourseMaterials => Set<CourseMaterial>();
    public DbSet<CourseTask> CourseTasks => Set<CourseTask>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>().Property(p => p.Price).HasPrecision(12, 2);
        builder.Entity<Course>().Property(c => c.Price).HasPrecision(12, 2);
        builder.Entity<Order>().Property(o => o.Total).HasPrecision(12, 2);
        builder.Entity<OrderItem>().Property(i => i.UnitPrice).HasPrecision(12, 2);
        builder.Entity<Invoice>().Property(i => i.Total).HasPrecision(12, 2);

        builder.Entity<Supplier>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Product>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Course>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Enrollment>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<CourseMaterial>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<CourseTask>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<BlogPost>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Order>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Invoice>().HasQueryFilter(x => !x.IsDeleted);
    }
}
