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

        [HttpGet]
        public IActionResult Create() => View(new Testimonial());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Testimonial model)
        {
            if (!ModelState.IsValid) return View(model);

            model.CreatedAt = DateTime.UtcNow;
            _context.Testimonials.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Testimonial created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t == null) return NotFound();
            return View(t);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Testimonial model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var existing = await _context.Testimonials.FindAsync(id);
            if (existing == null) return NotFound();

            existing.AuthorName = model.AuthorName;
            existing.AuthorTitle = model.AuthorTitle;
            existing.AuthorAvatarUrl = model.AuthorAvatarUrl;
            existing.Content = model.Content;
            existing.Rating = Math.Clamp(model.Rating, 1, 5);
            existing.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Testimonial updated.";
            return RedirectToAction("Index");
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
