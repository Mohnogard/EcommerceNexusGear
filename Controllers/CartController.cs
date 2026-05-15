using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace NexusGear.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string GuestCartKey = "GuestCart";

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private Dictionary<int, int> GetGuestCart()
        {
            var json = HttpContext.Session.GetString(GuestCartKey);
            return json != null
                ? JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new()
                : new();
        }

        private void SaveGuestCart(Dictionary<int, int> cart)
        {
            HttpContext.Session.SetString(GuestCartKey, JsonSerializer.Serialize(cart));
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                var guestCart = GetGuestCart();
                var vm = new CartViewModel();
                if (guestCart.Any())
                {
                    var productIds = guestCart.Keys.ToList();
                    var products = await _context.Products
                        .Include(p => p.Category)
                        .Where(p => productIds.Contains(p.Id) && p.IsActive)
                        .ToDictionaryAsync(p => p.Id);

                    vm.Items = guestCart
                        .Where(kv => products.ContainsKey(kv.Key))
                        .Select(kv => new CartItem
                        {
                            Id = -kv.Key,
                            ProductId = kv.Key,
                            Product = products[kv.Key],
                            Quantity = kv.Value
                        }).ToList();
                }
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = await _context.CartItems
                .Include(c => c.Product).ThenInclude(p => p!.Category)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(new CartViewModel { Items = items });
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
                return Json(new { success = false, message = "Product not available" });

            if (product.Stock <= 0)
                return Json(new { success = false, message = "This product is out of stock" });

            if (User.Identity?.IsAuthenticated != true)
            {
                var guestCart = GetGuestCart();
                int currentQty = guestCart.GetValueOrDefault(productId, 0);
                int newQty = currentQty + quantity;
                if (newQty > product.Stock)
                    return Json(new { success = false, message = $"Only {product.Stock} item(s) available in stock" });

                guestCart[productId] = newQty;
                SaveGuestCart(guestCart);
                return Json(new { success = true, cartCount = guestCart.Values.Sum() });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            int newQtyDb = (existing?.Quantity ?? 0) + quantity;
            if (newQtyDb > product.Stock)
                return Json(new { success = false, message = $"Only {product.Stock} item(s) available in stock" });

            if (existing != null)
                existing.Quantity = newQtyDb;
            else
                _context.CartItems.Add(new CartItem { UserId = userId, ProductId = productId, Quantity = quantity });

            await _context.SaveChangesAsync();
            int cartCount = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new { success = true, cartCount });
        }

        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                int productId = -cartItemId;
                var guestCart = GetGuestCart();
                if (!guestCart.ContainsKey(productId))
                    return Json(new { success = false });

                var product = await _context.Products.FindAsync(productId);
                int stock = product?.Stock ?? 0;

                if (quantity <= 0)
                    guestCart.Remove(productId);
                else if (quantity > stock)
                    return Json(new { success = false, message = $"Only {stock} in stock", maxQty = stock });
                else
                    guestCart[productId] = quantity;

                SaveGuestCart(guestCart);

                var pids = guestCart.Keys.ToList();
                var prices = pids.Any()
                    ? await _context.Products.Where(p => pids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.DiscountedPrice)
                    : new Dictionary<int, decimal>();
                decimal subtotal = guestCart.Sum(kv => prices.GetValueOrDefault(kv.Key) * kv.Value);
                decimal shipping = subtotal > 0 && subtotal < 50 ? 9.99m : 0;
                decimal itemTotal = product != null ? product.DiscountedPrice * (quantity > 0 ? quantity : 0) : 0;

                return Json(new
                {
                    success = true,
                    itemTotal,
                    subtotal,
                    shipping,
                    total = subtotal + shipping,
                    cartCount = guestCart.Values.Sum()
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var item = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (item == null) return Json(new { success = false });

            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                int stockDb = item.Product?.Stock ?? 0;
                if (quantity > stockDb)
                    return Json(new { success = false, message = $"Only {stockDb} in stock", maxQty = stockDb });
                item.Quantity = quantity;
            }

            await _context.SaveChangesAsync();
            var cart = await BuildCartViewModel(userId);
            return Json(new
            {
                success = true,
                itemTotal = item.Product != null ? item.Product.DiscountedPrice * (quantity > 0 ? quantity : 0) : 0,
                subtotal = cart.Subtotal,
                shipping = cart.ShippingCost,
                total = cart.Total,
                cartCount = cart.ItemCount
            });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                int productId = -cartItemId;
                var guestCart = GetGuestCart();
                guestCart.Remove(productId);
                SaveGuestCart(guestCart);

                var pids = guestCart.Keys.ToList();
                var prices = pids.Any()
                    ? await _context.Products.Where(p => pids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.DiscountedPrice)
                    : new Dictionary<int, decimal>();
                decimal subtotal = guestCart.Sum(kv => prices.GetValueOrDefault(kv.Key) * kv.Value);
                decimal shipping = subtotal > 0 && subtotal < 50 ? 9.99m : 0;

                return Json(new
                {
                    success = true,
                    subtotal,
                    shipping,
                    total = subtotal + shipping,
                    cartCount = guestCart.Values.Sum()
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item != null) { _context.CartItems.Remove(item); await _context.SaveChangesAsync(); }

            var cart2 = await BuildCartViewModel(userId);
            return Json(new { success = true, subtotal = cart2.Subtotal, shipping = cart2.ShippingCost, total = cart2.Total, cartCount = cart2.ItemCount });
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                var guestCart = GetGuestCart();
                return Json(new { count = guestCart.Values.Sum() });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            int count = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new { count });
        }

        private async Task<CartViewModel> BuildCartViewModel(string userId)
        {
            var items = await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            return new CartViewModel { Items = items };
        }
    }
}
