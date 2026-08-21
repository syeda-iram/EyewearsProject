using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize(Roles = "Vendor")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var vendor = await VendorContext.GetCurrentVendorAsync(_context, _userManager, User);
            if (vendor == null) return RedirectToAction("AccessDenied", "Account");

            ViewBag.VendorName = vendor.CompanyName;
            ViewBag.TotalPOs = await _context.PurchaseOrders.CountAsync(po => po.VendorId == vendor.Id);
            ViewBag.PendingPOs = await _context.PurchaseOrders.CountAsync(po => po.VendorId == vendor.Id && po.Status != PurchaseOrderStatus.Received && po.Status != PurchaseOrderStatus.Cancelled);
            ViewBag.UnpaidInvoices = await _context.Invoices
                .Include(i => i.PurchaseOrder)
                .CountAsync(i => i.PurchaseOrder.VendorId == vendor.Id && i.Status != InvoiceStatus.Paid);

            ViewData["Title"] = "Dashboard";
            return View();
        }
    }
}