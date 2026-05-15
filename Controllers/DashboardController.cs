using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;
using System.Security.Claims;

namespace NexusGear.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userManager.GetUserAsync(User);

            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var vm = new UserDashboardViewModel
            {
                User = user!,
                RecentOrders = orders.Take(5).ToList(),
                TotalOrders = orders.Count,
                TotalSpent = orders.Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded).Sum(o => o.Total),
                WishlistCount = await _context.WishlistItems.CountAsync(w => w.UserId == userId)
            };

            return View(vm);
        }

        public async Task<IActionResult> Orders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p!.Category)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> Wishlist()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = await _context.WishlistItems
                .Include(w => w.Product).ThenInclude(p => p!.Category)
                .Include(w => w.Product).ThenInclude(p => p!.Reviews)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Json(new { success = false, requiresLogin = true });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var existing = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            bool added;
            if (existing != null)
            {
                _context.WishlistItems.Remove(existing);
                added = false;
            }
            else
            {
                _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
                added = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, added });
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            var vm = new EditProfileViewModel
            {
                FirstName = user?.FirstName ?? string.Empty,
                LastName = user?.LastName ?? string.Empty,
                Phone = user?.PhoneNumber,
                Address = user?.Address,
                City = user?.City,
                Country = user?.Country,
                PostalCode = user?.PostalCode
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditProfileViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = vm.FirstName;
            user.LastName = vm.LastName;
            user.PhoneNumber = vm.Phone;
            user.Address = vm.Address;
            user.City = vm.City;
            user.Country = vm.Country;
            user.PostalCode = vm.PostalCode;

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }
    }
}
