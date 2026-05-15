using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Services;
using NexusGear.ViewModels;
using System.Security.Claims;

namespace NexusGear.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripeService _stripeService;
        private readonly IConfiguration _config;

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, StripeService stripeService, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _stripeService = stripeService;
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cartItems = await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            var user = await _userManager.GetUserAsync(User);
            decimal subtotal = cartItems.Sum(i => (i.Product?.DiscountedPrice ?? 0) * i.Quantity);
            decimal shipping = subtotal >= 50 ? 0 : 9.99m;

            var vm = new CheckoutViewModel
            {
                FirstName = user?.FirstName ?? string.Empty,
                LastName = user?.LastName ?? string.Empty,
                Email = user?.Email ?? string.Empty,
                Phone = user?.PhoneNumber ?? string.Empty,
                Address = user?.Address ?? string.Empty,
                City = user?.City ?? string.Empty,
                Country = user?.Country ?? string.Empty,
                PostalCode = user?.PostalCode ?? string.Empty,
                Subtotal = subtotal,
                ShippingCost = shipping,
                Total = subtotal + shipping
            };

            ViewBag.StripePublishableKey = _config["Stripe:PublishableKey"];
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreateIntentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cartItems = await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            if (!cartItems.Any()) return BadRequest();

            decimal subtotal = cartItems.Sum(i => (i.Product?.DiscountedPrice ?? 0) * i.Quantity);
            decimal shipping = subtotal >= 50 ? 0 : 9.99m;
            decimal total = subtotal + shipping;

            var intent = await _stripeService.CreatePaymentIntentAsync(total, "usd", $"NexusGear order for {userId}");
            return Json(new { clientSecret = intent.ClientSecret, intentId = intent.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.StripePublishableKey = _config["Stripe:PublishableKey"];
                return View("Index", vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cartItems = await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            decimal subtotal = cartItems.Sum(i => (i.Product?.DiscountedPrice ?? 0) * i.Quantity);
            decimal shipping = subtotal >= 50 ? 0 : 9.99m;

            var paymentStatus = PaymentStatus.Pending;
            if (vm.PaymentMethod == "card" && !string.IsNullOrEmpty(vm.StripePaymentIntentId))
            {
                try
                {
                    var intent = await _stripeService.GetPaymentIntentAsync(vm.StripePaymentIntentId);
                    paymentStatus = intent.Status == "succeeded" ? PaymentStatus.Paid : PaymentStatus.Failed;
                    if (paymentStatus == PaymentStatus.Failed)
                    {
                        ModelState.AddModelError("", "Payment was not successful. Please try again.");
                        ViewBag.StripePublishableKey = _config["Stripe:PublishableKey"];
                        vm.Subtotal = subtotal; vm.ShippingCost = shipping; vm.Total = subtotal + shipping;
                        return View("Index", vm);
                    }
                }
                catch { paymentStatus = PaymentStatus.Failed; }
            }
            else if (vm.PaymentMethod == "cod")
            {
                paymentStatus = PaymentStatus.Pending;
            }

            var order = new Order
            {
                OrderNumber = $"NX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                UserId = userId,
                ShippingAddress = vm.Address,
                ShippingCity = vm.City,
                ShippingCountry = vm.Country,
                ShippingPostalCode = vm.PostalCode,
                ShippingPhone = vm.Phone,
                PaymentMethod = vm.PaymentMethod,
                StripePaymentIntentId = vm.StripePaymentIntentId,
                Notes = vm.Notes,
                Subtotal = subtotal,
                ShippingCost = shipping,
                Total = subtotal + shipping,
                Status = OrderStatus.Confirmed,
                PaymentStatus = paymentStatus,
                OrderItems = cartItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product?.DiscountedPrice ?? 0,
                    Total = (i.Product?.DiscountedPrice ?? 0) * i.Quantity,
                    ProductName = i.Product?.Name
                }).ToList()
            };

            // Decrement stock
            foreach (var item in cartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null) product.Stock = Math.Max(0, product.Stock - item.Quantity);
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("Confirmation", new { id = order.Id });
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();
            return View(order);
        }
    }

    public class CreateIntentRequest
    {
        public string? PaymentMethod { get; set; }
    }
}
