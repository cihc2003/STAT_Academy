using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Web.Data;
using STAT_Academy.Web.Models;

namespace STAT_Academy.Web.Controllers;

[Authorize(Roles = SeedData.TutorRole)]
public class TutorController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public TutorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Courses()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(await _db.Courses.Where(c => c.TutorId == user!.Id).OrderBy(c => c.Title).ToListAsync());
    }

    public async Task<IActionResult> Manage(int id)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
        ViewBag.Materials = await _db.CourseMaterials.Where(m => m.CourseId == id).OrderBy(m => m.Week).ToListAsync();
        ViewBag.Tasks = await _db.CourseTasks.Where(t => t.CourseId == id).OrderBy(t => t.DueDate).ToListAsync();
        ViewBag.Students = await _db.Enrollments.Include(e => e.Student).Where(e => e.CourseId == id && e.Status == "Aprobada").ToListAsync();
        return course == null ? NotFound() : View(course);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaterial(CourseMaterial model)
    {
        if (ModelState.IsValid)
        {
            _db.CourseMaterials.Add(model);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Manage), new { id = model.CourseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTask(CourseTask model)
    {
        if (ModelState.IsValid)
        {
            _db.CourseTasks.Add(model);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Manage), new { id = model.CourseId });
    }
}
