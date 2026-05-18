using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;

namespace NexusGear.ViewComponents
{
    public class NavCategoriesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NavCategoriesViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Take(12)
                .ToListAsync();

            return View(categories);
        }
    }
}
