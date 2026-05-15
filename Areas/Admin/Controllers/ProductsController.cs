using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;

namespace NexusGear.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string? q, int? categoryId)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrEmpty(q)) query = query.Where(p => p.Name.Contains(q) || (p.Brand != null && p.Brand.Contains(q)));
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Q = q;
            ViewBag.CategoryId = categoryId;
            return View(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            return View(new Product());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? mainImage, List<IFormFile>? additionalImages)
        {
            ModelState.Remove("Category");
            ModelState.Remove("Reviews");
            ModelState.Remove("OrderItems");
            ModelState.Remove("WishlistItems");
            ModelState.Remove("CartItems");
            ModelState.Remove("Images");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = model.Name.ToLower().Replace(" ", "-").Replace("'", "").Replace(",", "");

            if (mainImage != null && mainImage.Length > 0)
                model.ImageUrl = await SaveImageAsync(mainImage, "products");

            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            if (additionalImages != null)
            {
                foreach (var img in additionalImages.Where(f => f.Length > 0))
                {
                    var url = await SaveImageAsync(img, "products");
                    _context.ProductImages.Add(new ProductImage { ProductId = model.Id, ImageUrl = url });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Product created successfully.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? mainImage, List<IFormFile>? additionalImages)
        {
            if (id != model.Id) return BadRequest();
            ModelState.Remove("Category");
            ModelState.Remove("Reviews");
            ModelState.Remove("OrderItems");
            ModelState.Remove("WishlistItems");
            ModelState.Remove("CartItems");
            ModelState.Remove("Images");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", model.CategoryId);
                return View(model);
            }

            var existing = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.DiscountPercentage = model.DiscountPercentage;
            existing.Stock = model.Stock;
            existing.Brand = model.Brand;
            existing.Sku = model.Sku;
            existing.CategoryId = model.CategoryId;
            existing.IsActive = model.IsActive;
            existing.IsFeatured = model.IsFeatured;
            existing.IsNewArrival = model.IsNewArrival;
            if (string.IsNullOrWhiteSpace(model.Slug))
                existing.Slug = model.Name.ToLower().Replace(" ", "-").Replace("'", "").Replace(",", "");
            else
                existing.Slug = model.Slug;

            if (mainImage != null && mainImage.Length > 0)
                existing.ImageUrl = await SaveImageAsync(mainImage, "products");

            if (additionalImages != null)
            {
                foreach (var img in additionalImages.Where(f => f.Length > 0))
                {
                    var url = await SaveImageAsync(img, "products");
                    _context.ProductImages.Add(new ProductImage { ProductId = id, ImageUrl = url });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product updated.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null) { product.IsActive = false; await _context.SaveChangesAsync(); }
            TempData["Success"] = "Product removed.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var img = await _context.ProductImages.FindAsync(imageId);
            if (img != null) { _context.ProductImages.Remove(img); await _context.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        private async Task<string> SaveImageAsync(IFormFile file, string folder)
        {
            var uploads = Path.Combine(_env.WebRootPath, "images", folder);
            Directory.CreateDirectory(uploads);
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(uploads, fileName);
            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/images/{folder}/{fileName}";
        }
    }
}
