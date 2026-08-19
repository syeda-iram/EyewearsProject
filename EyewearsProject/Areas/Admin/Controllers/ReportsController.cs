using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.FinanceModule)]
    public class ReportsController : Controller
    {
        private const int LowStockThreshold = 10;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var vm = new ReportsViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders
                    .Where(o => o.PaymentStatus == PaymentStatus.Paid)
                    .SumAsync(o => (decimal?)o.GrandTotal) ?? 0,
                PendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Pending),
                DeliveredOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Delivered),
                CancelledOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Cancelled),

                TotalUsers = await _userManager.Users.CountAsync(),
                ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive),
                NewUsersLast30Days = await _userManager.Users.CountAsync(u => u.CreatedAt >= thirtyDaysAgo),

                TotalProducts = await _context.Products.CountAsync(),
                ActiveProducts = await _context.Products.CountAsync(p => p.IsActive),
                OutOfStockVariants = await _context.ProductVariants.CountAsync(v => v.StockQuantity == 0),
                LowStockVariants = await _context.ProductVariants.CountAsync(v => v.StockQuantity > 0 && v.StockQuantity <= LowStockThreshold),

                ActivePromotions = await _context.Promotions.CountAsync(p => p.IsActive && p.EndDate >= DateTime.UtcNow),
                TotalPromotionUsage = await _context.Promotions.SumAsync(p => (int?)p.UsageCount) ?? 0,

                PendingReturns = await _context.Returns.CountAsync(r => r.Status == ReturnStatus.Requested),
                TotalRefunded = await _context.Returns
                    .Where(r => r.Status == ReturnStatus.Refunded)
                    .SumAsync(r => (decimal?)r.RefundAmount) ?? 0
            };

            vm.AverageOrderValue = vm.TotalOrders > 0 ? vm.TotalRevenue / vm.TotalOrders : 0;

            vm.TopProducts = await _context.OrderItems
                .GroupBy(oi => oi.ProductName)
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key,
                    UnitsSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(p => p.UnitsSold)
                .Take(5)
                .ToListAsync();

            ViewData["Title"] = "Reports";
            return View(vm);
        }
    }
}