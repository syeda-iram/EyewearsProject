using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.AllAdmins)]
    public class DashboardController : Controller
    {
        private const int LowStockThreshold = 10;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearAgo = today.AddDays(-365);
            var last12MonthsStart = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
            var last30Days = today.AddDays(-29);

            var paidOrders = _context.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid);

            var vm = new DashboardViewModel
            {
                TotalSales = await paidOrders.SumAsync(o => (decimal?)o.GrandTotal) ?? 0,
                TodaySales = await paidOrders.Where(o => o.OrderDate >= today).SumAsync(o => (decimal?)o.GrandTotal) ?? 0,
                MonthlySales = await paidOrders.Where(o => o.OrderDate >= monthStart).SumAsync(o => (decimal?)o.GrandTotal) ?? 0,
                TotalRefunded = await _context.Returns.Where(r => r.Status == ReturnStatus.Refunded).SumAsync(r => (decimal?)r.RefundAmount) ?? 0,

                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Pending),
                DeliveredOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Delivered),
                CancelledOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Cancelled),
                TotalReturns = await _context.Returns.CountAsync(),

                TotalCustomers = await _userManager.Users.CountAsync(),
                NewCustomersThisMonth = await _userManager.Users.CountAsync(u => u.CreatedAt >= monthStart),

                LowStockCount = await _context.ProductVariants.CountAsync(v => v.StockQuantity <= LowStockThreshold)
            };

            // Top products by units sold (paid orders only)
            vm.TopProducts = await _context.OrderItems
                .Where(oi => oi.Order.PaymentStatus == PaymentStatus.Paid)
                .GroupBy(oi => oi.ProductName)
                .Select(g => new TopProductRow
                {
                    ProductName = g.Key,
                    UnitsSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(p => p.UnitsSold)
                .Take(5)
                .ToListAsync();

            // Top brands by units sold
            vm.TopBrands = await _context.OrderItems
                .Where(oi => oi.Order.PaymentStatus == PaymentStatus.Paid)
                .Include(oi => oi.Product)
                .GroupBy(oi => oi.Product.Brand.Name)
                .Select(g => new TopBrandRow
                {
                    BrandName = g.Key,
                    UnitsSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(b => b.UnitsSold)
                .Take(5)
                .ToListAsync();

            // Sales by day — last 30 days
            var dailyRaw = await paidOrders
                .Where(o => o.OrderDate >= last30Days)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.GrandTotal) })
                .ToListAsync();

            for (var d = last30Days; d <= today; d = d.AddDays(1))
            {
                var match = dailyRaw.FirstOrDefault(x => x.Date == d);
                vm.SalesByDay.Add(new ChartPoint { Label = d.ToString("dd MMM"), Value = match?.Total ?? 0 });
            }

            // Sales by month — last 12 months
            var monthlyRaw = await paidOrders
                .Where(o => o.OrderDate >= last12MonthsStart)
                .ToListAsync(); // pulled client-side to group by Y/M safely across providers

            for (var m = last12MonthsStart; m <= today; m = m.AddMonths(1))
            {
                var monthTotal = monthlyRaw.Where(o => o.OrderDate.Year == m.Year && o.OrderDate.Month == m.Month).Sum(o => o.GrandTotal);
                vm.SalesByMonth.Add(new ChartPoint { Label = m.ToString("MMM yyyy"), Value = monthTotal });
            }

            // Sales by category
            vm.SalesByCategory = await _context.OrderItems
                .Where(oi => oi.Order.PaymentStatus == PaymentStatus.Paid)
                .Include(oi => oi.Product)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new ChartPoint { Label = g.Key, Value = g.Sum(oi => oi.UnitPrice * oi.Quantity) })
                .OrderByDescending(c => c.Value)
                .ToListAsync();

            // Sales by brand (reuse TopBrands data, same source)
            vm.SalesByBrand = vm.TopBrands.Select(b => new ChartPoint { Label = b.BrandName, Value = b.Revenue }).ToList();

            // Customer growth — last 12 months, cumulative
            var allUsersCreated = await _userManager.Users
                .Where(u => u.CreatedAt >= last12MonthsStart)
                .Select(u => u.CreatedAt)
                .ToListAsync();

            var baselineCount = await _userManager.Users.CountAsync(u => u.CreatedAt < last12MonthsStart);
            var running = baselineCount;

            for (var m = last12MonthsStart; m <= today; m = m.AddMonths(1))
            {
                running += allUsersCreated.Count(d => d.Year == m.Year && d.Month == m.Month);
                vm.CustomerGrowth.Add(new ChartPoint { Label = m.ToString("MMM yyyy"), Value = running });
            }

            // Orders by status
            vm.OrdersByStatus = await _context.Orders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new ChartPoint { Label = g.Key.ToString(), Value = g.Count() })
                .ToListAsync();

            ViewData["Title"] = "Dashboard";
            return View(vm);
        }
    }
}