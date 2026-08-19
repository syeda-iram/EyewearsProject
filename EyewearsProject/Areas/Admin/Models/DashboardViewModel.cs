using EyewearsProject.Models;

namespace EyewearsProject.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBrands { get; set; }

        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }

        public int OutOfStockVariants { get; set; }
        public int LowStockVariants { get; set; }

        public int PendingReturns { get; set; }
        public int PendingReviews { get; set; }

        public List<Order> RecentOrders { get; set; } = new();
        public List<AuditLog> RecentActivity { get; set; } = new();
    }
}