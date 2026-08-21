using EyewearsProject.Models;

namespace EyewearsProject.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        // Sales
        public decimal TotalSales { get; set; }
        public decimal TodaySales { get; set; }
        public decimal MonthlySales { get; set; }
        public decimal TotalRefunded { get; set; }

        // Orders
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int TotalReturns { get; set; }

        // Customers
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }

        // Products
        public int LowStockCount { get; set; }
        public List<TopProductRow> TopProducts { get; set; } = new();
        public List<TopBrandRow> TopBrands { get; set; } = new();

        // Chart data
        public List<ChartPoint> SalesByDay { get; set; } = new();
        public List<ChartPoint> SalesByMonth { get; set; } = new();
        public List<ChartPoint> SalesByCategory { get; set; } = new();
        public List<ChartPoint> SalesByBrand { get; set; } = new();
        public List<ChartPoint> CustomerGrowth { get; set; } = new();
        public List<ChartPoint> OrdersByStatus { get; set; } = new();
    }

    public class TopProductRow
    {
        public string ProductName { get; set; } = "";
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopBrandRow
    {
        public string BrandName { get; set; } = "";
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ChartPoint
    {
        public string Label { get; set; } = "";
        public decimal Value { get; set; }
    }
}