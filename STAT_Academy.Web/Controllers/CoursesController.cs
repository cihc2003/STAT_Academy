using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Data;
using STAT_Academy.Web.Models;

namespace STAT_Academy.Web.Controllers;

public class CoursesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CoursesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, string? category)
    {
        var query = _db.Courses.Include(c => c.Tutor).Where(c => c.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Title.Contains(search));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(c => c.Category == category);
        ViewBag.Categories = await _db.Courses.Where(c => c.IsActive).Select(c => c.Category).Distinct().ToListAsync();
        return View(await query.OrderBy(c => c.Title).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var course = await _db.Courses.Include(c => c.Tutor).FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        return course == null ? NotFound() : View(course);
    }

    [Authorize(Roles = SeedData.StudentRole)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var exists = await _db.Enrollments.AnyAsync(e => e.CourseId == id && e.StudentId == user.Id);
        if (!exists)
        {
            _db.Enrollments.Add(new Enrollment { CourseId = id, StudentId = user.Id });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Solicitud de matricula enviada.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
