using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.ViewModels;
using System.Security.Claims;

namespace NexusGear.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 12;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, string? q, string? sortBy, bool onlyDiscounted = false, decimal? minPrice = null, decimal? maxPrice = null, int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q) || (p.Brand != null && p.Brand.Contains(q)) || (p.Description != null && p.Description.Contains(q)));

            if (onlyDiscounted)
                query = query.Where(p => p.DiscountPercentage > 0);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sortBy switch
            {
                "price_asc"  => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest"     => query.OrderByDescending(p => p.CreatedAt),
                "discount"   => query.OrderByDescending(p => p.DiscountPercentage),
                "rating"     => query.OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0),
                _            => query.OrderBy(p => p.Name)
            };

            int total = await query.CountAsync();
            var products = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            var vm = new ShopViewModel
            {
                Products = products,
                Categories = await _context.Categories
                    .Include(c => c.Products)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync(),
                SelectedCategoryId = categoryId,
                SearchQuery = q,
                SortBy = sortBy,
                OnlyDiscounted = onlyDiscounted,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Page = page,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize)
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Product(string slug)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews.Where(r => r.IsApproved)).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null) return NotFound();

            bool isInWishlist = false;
            bool userHasReviewed = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                isInWishlist = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == product.Id);
                userHasReviewed = await _context.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == product.Id);
            }

            var related = await _context.Products
                .Include(p => p.Reviews)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .Take(4)
                .ToListAsync();

            return View(new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = related,
                IsInWishlist = isInWishlist,
                UserHasReviewed = userHasReviewed
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string? content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            bool alreadyReviewed = await _context.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == productId);
            if (!alreadyReviewed)
            {
                _context.Reviews.Add(new Models.Review
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = Math.Clamp(rating, 1, 5),
                    Content = content,
                    IsApproved = false
                });
                await _context.SaveChangesAsync();
                TempData["ReviewMsg"] = "Your review has been submitted and is awaiting approval.";
            }

            return RedirectToAction("Product", new { slug = product.Slug });
        }
    }
}
