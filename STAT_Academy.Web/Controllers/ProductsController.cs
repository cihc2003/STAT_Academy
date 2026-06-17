using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Data;

namespace STAT_Academy.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, string? category)
    {
        var query = _db.Products.Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Name.Contains(search));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(p => p.Category == category);
        ViewBag.Categories = await _db.Products.Where(p => p.IsActive).Select(p => p.Category).Distinct().ToListAsync();
        return View(await query.OrderBy(p => p.Name).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        return product == null ? NotFound() : View(product);
    }
}
