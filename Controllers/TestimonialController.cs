using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NexusGear.Data;
using NexusGear.Models;

namespace NexusGear.Controllers
{
    public class TestimonialController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TestimonialController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Submit(string authorTitle, string content, int rating)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["TestimonialError"] = "Please write something before submitting.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            _context.Testimonials.Add(new Testimonial
            {
                AuthorName = user.FullName,
                AuthorTitle = string.IsNullOrWhiteSpace(authorTitle) ? "NexusGear Customer" : authorTitle,
                Content = content.Trim(),
                Rating = Math.Clamp(rating, 1, 5),
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["TestimonialMsg"] = "Thank you! Your testimonial has been submitted and is awaiting approval.";
            return RedirectToAction("Index", "Home");
        }
    }
}
