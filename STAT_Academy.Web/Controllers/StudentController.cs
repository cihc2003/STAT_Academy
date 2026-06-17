using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Data;
using STAT_Academy.Web.Models;

namespace STAT_Academy.Web.Controllers;

[Authorize(Roles = SeedData.StudentRole)]
public class StudentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> MisCursos()
    {
        var user = await _userManager.GetUserAsync(User);
        var enrollments = await _db.Enrollments.Include(e => e.Course).Where(e => e.StudentId == user!.Id).ToListAsync();
        return View(enrollments);
    }

    public async Task<IActionResult> Course(int id)
    {
        var course = await _db.Courses.Include(c => c.Tutor).FirstOrDefaultAsync(c => c.Id == id);
        ViewBag.Materials = await _db.CourseMaterials.Where(m => m.CourseId == id).OrderBy(m => m.Week).ToListAsync();
        ViewBag.Tasks = await _db.CourseTasks.Where(t => t.CourseId == id).OrderBy(t => t.DueDate).ToListAsync();
        return course == null ? NotFound() : View(course);
    }

    public async Task<IActionResult> Orders()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product).Where(o => o.UserId == user!.Id).OrderByDescending(o => o.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Invoices()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(await _db.Invoices.Include(i => i.Order).Where(i => i.Order!.UserId == user!.Id).OrderByDescending(i => i.IssuedAt).ToListAsync());
    }
}
