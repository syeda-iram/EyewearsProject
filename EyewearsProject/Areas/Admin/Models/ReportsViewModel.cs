namespace EyewearsProject.Areas.Admin.Models
{
    public class ReportsViewModel
    {
        // Sales
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        // Users
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersLast30Days { get; set; }

        // Products
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStockVariants { get; set; }
        public int LowStockVariants { get; set; }

        // Promotions
        public int ActivePromotions { get; set; }
        public int TotalPromotionUsage { get; set; }

        // Returns
        public int PendingReturns { get; set; }
        public decimal TotalRefunded { get; set; }

        // Top products by units sold
        public List<TopProductViewModel> TopProducts { get; set; } = new();
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; } = "";
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}