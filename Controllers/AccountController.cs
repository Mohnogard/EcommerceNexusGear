using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;
using System.Text.Json;

namespace NexusGear.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(vm.Email);
                await MergeGuestCartAsync(user?.Id);

                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                return LocalRedirect(vm.ReturnUrl ?? "/");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError("", "Account locked. Try again in a few minutes.");
            else
                ModelState.AddModelError("", "Invalid email or password.");

            return View(vm);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                await _signInManager.SignInAsync(user, isPersistent: false);
                await MergeGuestCartAsync(user.Id);
                return LocalRedirect(vm.ReturnUrl ?? "/");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();

        private async Task MergeGuestCartAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var json = HttpContext.Session.GetString("GuestCart");
            if (string.IsNullOrEmpty(json)) return;

            var guestCart = JsonSerializer.Deserialize<Dictionary<int, int>>(json);
            if (guestCart == null || !guestCart.Any()) return;

            foreach (var kv in guestCart)
            {
                var product = await _context.Products.FindAsync(kv.Key);
                if (product == null || !product.IsActive) continue;

                var existing = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == kv.Key);

                int newQty = Math.Min((existing?.Quantity ?? 0) + kv.Value, product.Stock);
                if (newQty <= 0) continue;

                if (existing != null)
                    existing.Quantity = newQty;
                else
                    _context.CartItems.Add(new CartItem { UserId = userId, ProductId = kv.Key, Quantity = newQty });
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("GuestCart");
        }
    }
}
