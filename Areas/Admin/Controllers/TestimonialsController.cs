using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;

namespace NexusGear.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TestimonialsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestimonialsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(bool? pending)
        {
            var query = _context.Testimonials.AsQueryable();
            if (pending == true) query = query.Where(t => !t.IsActive);
            ViewBag.Pending = pending;
            return View(await query.OrderByDescending(t => t.CreatedAt).ToListAsync());
        }

       

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t != null) { t.IsActive = true; await _context.SaveChangesAsync(); }
            return RedirectToAction("Index", new { pending = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t != null) { t.IsActive = false; await _context.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t != null) { _context.Testimonials.Remove(t); await _context.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }
    }
}
