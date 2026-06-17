using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Models;

namespace STAT_Academy.Web.Data;

public static class SeedData
{
    public const string AdminRole = "Administrador";
    public const string TutorRole = "Tutor";
    public const string StudentRole = "Estudiante";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        foreach (var role in new[] { AdminRole, TutorRole, StudentRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(userManager, "admin@statmedical.com", "Administrador STATMEDICAL", "Admin123!", AdminRole);
        var tutor = await EnsureUserAsync(userManager, "tutor@statmedical.com", "Tutor STAT Academy", "Tutor123!", TutorRole);
        await EnsureUserAsync(userManager, "estudiante@statmedical.com", "Estudiante Demo", "Estudiante123!", StudentRole);

        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.AddRange(
                new Supplier { Name = "STATMEDICAL Insumos", Email = "proveedores@statmedical.com", Phone = "+502 2412 8800", Address = "Ciudad de Guatemala" },
                new Supplier { Name = "Insumos Educativos de Salud", Email = "ventas@insumossalud.com", Phone = "+502 2200 1188", Address = "Zona 10" });
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Name = "Kit de signos vitales", Category = "Equipo médico", Description = "Kit básico para prácticas clínicas y simulación.", Price = 349, Stock = 20 },
                new Product { Name = "Manual de emergencias", Category = "Libros", Description = "Guía educativa para protocolos de atención inmediata.", Price = 39, Stock = 60 },
                new Product { Name = "Paquete de simulación", Category = "Educación", Description = "Materiales didácticos para talleres de salud.", Price = 89, Stock = 35 });
        }

        if (!await db.Courses.AnyAsync())
        {
            db.Courses.AddRange(
                new Course { Title = "Soporte Vital Básico", Category = "Salud", Description = "Fundamentos de atención primaria, RCP y respuesta inicial.", DurationWeeks = 6, Price = 120, TutorId = tutor.Id },
                new Course { Title = "Análisis de Datos en Salud", Category = "Datos", Description = "Indicadores, reportes y decisiones basadas en evidencia clínica.", DurationWeeks = 8, Price = 150, TutorId = tutor.Id },
                new Course { Title = "Gestión de Clínicas", Category = "Administración", Description = "Procesos, calidad y administración para servicios de salud.", DurationWeeks = 5, Price = 95, TutorId = tutor.Id });
        }

        if (!await db.BlogPosts.AnyAsync())
        {
            db.BlogPosts.AddRange(
                new BlogPost { Title = "Cómo prepararte para una emergencia", Category = "Prevención", Summary = "Principios básicos para actuar con calma y criterio.", Content = "La preparación salva tiempo y reduce riesgos. Organiza contactos, botiquín y protocolos familiares." },
                new BlogPost { Title = "Datos que mejoran la salud", Category = "Analítica", Summary = "El valor de medir indicadores clínicos y educativos.", Content = "Los datos ayudan a detectar patrones, priorizar recursos y evaluar resultados en programas de salud." });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string fullName, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                PhoneNumber = "+502 0000 0000"
            };
            await userManager.CreateAsync(user, password);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
