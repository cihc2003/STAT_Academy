using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Data;
using STAT_Academy.Web.Models;
using System.Diagnostics;

namespace STAT_Academy.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.FeaturedCourses = await _db.Courses.Include(c => c.Tutor).Where(c => c.IsActive).Take(3).ToListAsync();
        ViewBag.FeaturedProducts = await _db.Products.Where(p => p.IsActive).Take(3).ToListAsync();
        ViewBag.FeaturedPosts = await _db.BlogPosts.Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt).Take(3).ToListAsync();
        return View();
    }

    public IActionResult Contacto() => View();
    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
