using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize(Roles = "Vendor")]
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InvoicesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Vendor/Invoices
        public async Task<IActionResult> Index()
        {
            var vendor = await VendorContext.GetCurrentVendorAsync(_context, _userManager, User);
            if (vendor == null) return RedirectToAction("AccessDenied", "Account");

            var invoices = await _context.Invoices
                .Include(i => i.PurchaseOrder)
                .Where(i => i.PurchaseOrder.VendorId == vendor.Id)
                .OrderByDescending(i => i.IssuedDate)
                .ToListAsync();

            ViewData["Title"] = "Invoices";
            return View(invoices);
        }
    }
}