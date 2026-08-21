using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize(Roles = "Vendor")]
    public class PurchaseOrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PurchaseOrdersController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Vendor/PurchaseOrders
        public async Task<IActionResult> Index()
        {
            var vendor = await VendorContext.GetCurrentVendorAsync(_context, _userManager, User);
            if (vendor == null) return RedirectToAction("AccessDenied", "Account");

            var orders = await _context.PurchaseOrders
                .Where(po => po.VendorId == vendor.Id)
                .OrderByDescending(po => po.OrderDate)
                .ToListAsync();

            ViewData["Title"] = "Purchase Orders";
            return View(orders);
        }

        // GET: /Vendor/PurchaseOrders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vendor = await VendorContext.GetCurrentVendorAsync(_context, _userManager, User);
            if (vendor == null) return RedirectToAction("AccessDenied", "Account");

            var po = await _context.PurchaseOrders
                .Include(p => p.Items)
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id && p.VendorId == vendor.Id);

            // The VendorId filter above is the critical isolation check —
            // a vendor requesting another vendor's PO id gets a clean 404, never their data.
            if (po == null) return NotFound();

            ViewData["Title"] = po.PoNumber;
            return View(po);
        }
    }
}