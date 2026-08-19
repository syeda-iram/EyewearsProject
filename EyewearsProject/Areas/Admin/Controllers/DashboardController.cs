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
            var vm = new DashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                ActiveProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalCategories = await _context.Categories.CountAsync(),
                TotalBrands = await _context.Brands.CountAsync(),

                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders
                    .Where(o => o.PaymentStatus == PaymentStatus.Paid)
                    .SumAsync(o => (decimal?)o.GrandTotal) ?? 0,
                PendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Pending),

                OutOfStockVariants = await _context.ProductVariants.CountAsync(v => v.StockQuantity == 0),
                LowStockVariants = await _context.ProductVariants.CountAsync(v => v.StockQuantity > 0 && v.StockQuantity <= LowStockThreshold),

                PendingReturns = await _context.Returns.CountAsync(r => r.Status == ReturnStatus.Requested),
                PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved),

                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync(),

                RecentActivity = await _context.AuditLogs
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(6)
                    .ToListAsync()
            };

            ViewData["Title"] = "Dashboard";
            return View(vm);
        }
    }
}