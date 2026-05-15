using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;

namespace NexusGear.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var vm = new AdminDashboardViewModel
            {
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                    .SumAsync(o => (decimal?)o.Total) ?? 0,
                MonthlyRevenue = await _context.Orders
                    .Where(o => o.CreatedAt >= monthStart && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0,
                TotalOrders = await _context.Orders.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalUsers = await _context.Users.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed),
                PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved),
                OutOfStockCount = await _context.Products.CountAsync(p => p.IsActive && p.Stock <= 0),
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .ToListAsync()
            };

            // Revenue chart: last 6 months
            for (int i = 5; i >= 0; i--)
            {
                var d = now.AddMonths(-i);
                var start = new DateTime(d.Year, d.Month, 1);
                var end = start.AddMonths(1);
                var rev = await _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;
                var cnt = await _context.Orders.CountAsync(o => o.CreatedAt >= start && o.CreatedAt < end);
                vm.RevenueChartData.Add(new RevenueDataPoint { Month = d.ToString("MMM"), Revenue = rev, OrderCount = cnt });
            }

            // Category sales
            vm.CategorySalesData = await _context.OrderItems
                .Include(i => i.Product).ThenInclude(p => p!.Category)
                .GroupBy(i => i.Product!.Category!.Name)
                .Select(g => new CategorySalesPoint { CategoryName = g.Key, SalesCount = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Total) })
                .ToListAsync();

            return View(vm);
        }
    }
}
