using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Data;

namespace STAT_Academy.Web.Controllers;

public class BlogController : Controller
{
    private readonly ApplicationDbContext _db;

    public BlogController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.BlogPosts.Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        return post == null ? NotFound() : View(post);
    }
}
