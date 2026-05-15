using NexusGear.Models;

namespace NexusGear.ViewModels
{
    public class UserDashboardViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<Order> RecentOrders { get; set; } = new();
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int WishlistCount { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalUsers { get; set; }
        public int PendingOrders { get; set; }
        public int PendingReviews { get; set; }
        public int OutOfStockCount { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<RevenueDataPoint> RevenueChartData { get; set; } = new();
        public List<CategorySalesPoint> CategorySalesData { get; set; } = new();
        public List<(string Name, int Sales)> TopProducts { get; set; } = new();
    }

    public class RevenueDataPoint
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class CategorySalesPoint
    {
        public string CategoryName { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
