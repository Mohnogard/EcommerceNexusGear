using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;

namespace NexusGear.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CategoriesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index() =>
            View(await _context.Categories.Include(c => c.Products).OrderBy(c => c.DisplayOrder).ToListAsync());

        public IActionResult Create() => View(new Category());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model, IFormFile? image)
        {
            ModelState.Remove("Products");
            if (!ModelState.IsValid) return View(model);

            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = model.Name.ToLower().Replace(" ", "-");

            if (image != null && image.Length > 0)
                model.ImageUrl = await SaveImageAsync(image);

            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Category created.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model, IFormFile? image)
        {
            if (id != model.Id) return BadRequest();
            ModelState.Remove("Products");
            if (!ModelState.IsValid) return View(model);

            var existing = await _context.Categories.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Slug = string.IsNullOrWhiteSpace(model.Slug) ? model.Name.ToLower().Replace(" ", "-") : model.Slug;
            existing.DisplayOrder = model.DisplayOrder;
            existing.IsActive = model.IsActive;

            if (image != null && image.Length > 0)
                existing.ImageUrl = await SaveImageAsync(image);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Category updated.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat != null) { cat.IsActive = false; await _context.SaveChangesAsync(); }
            TempData["Success"] = "Category removed.";
            return RedirectToAction("Index");
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploads = Path.Combine(_env.WebRootPath, "images", "categories");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/images/categories/{fileName}";
        }
    }
}
