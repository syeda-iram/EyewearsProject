using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.VendorModule)]
    public class PurchaseOrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public PurchaseOrdersController(AppDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        private async Task PopulateVendorsAsync()
        {
            ViewBag.Vendors = new SelectList(
                await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.CompanyName).ToListAsync(),
                "Id", "CompanyName");
        }

        // GET: /Admin/PurchaseOrders?vendorId=5
        public async Task<IActionResult> Index(int? vendorId)
        {
            var query = _context.PurchaseOrders.Include(po => po.Vendor).AsQueryable();

            if (vendorId.HasValue)
                query = query.Where(po => po.VendorId == vendorId.Value);

            var orders = await query.OrderByDescending(po => po.OrderDate).ToListAsync();

            ViewBag.VendorId = vendorId;
            ViewBag.VendorName = vendorId.HasValue
                ? (await _context.Vendors.FindAsync(vendorId.Value))?.CompanyName
                : null;

            ViewData["Title"] = "Purchase Orders";
            return View(orders);
        }

        // GET: /Admin/PurchaseOrders/Create?vendorId=5
        public async Task<IActionResult> Create(int? vendorId)
        {
            await PopulateVendorsAsync();
            var model = new PurchaseOrderFormViewModel();
            if (vendorId.HasValue) model.VendorId = vendorId.Value;
            return View(model);
        }

        // POST: /Admin/PurchaseOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderFormViewModel model)
        {
            model.Items = (model.Items ?? new())
                .Where(i => !string.IsNullOrWhiteSpace(i.ItemDescription) && i.Quantity > 0)
                .ToList();

            if (!model.Items.Any())
                ModelState.AddModelError("", "Add at least one item to the purchase order.");

            if (!ModelState.IsValid)
            {
                await PopulateVendorsAsync();
                return View(model);
            }

            var po = new PurchaseOrder
            {
                PoNumber = "PO-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                VendorId = model.VendorId,
                OrderDate = DateTime.UtcNow,
                ExpectedDeliveryDate = model.ExpectedDeliveryDate,
                Status = PurchaseOrderStatus.Draft,
                Notes = model.Notes
            };

            foreach (var item in model.Items)
            {
                po.Items.Add(new PurchaseOrderItem
                {
                    ItemDescription = item.ItemDescription,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost
                });
            }

            po.TotalAmount = po.Items.Sum(i => i.TotalCost);

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "PurchaseOrder", po.Id.ToString(), $"Created {po.PoNumber} for vendor #{model.VendorId}, total {po.TotalAmount:C}");

            TempData["Success"] = $"Purchase order {po.PoNumber} created.";
            return RedirectToAction(nameof(Details), new { id = po.Id });
        }

        // GET: /Admin/PurchaseOrders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Vendor)
                .Include(p => p.Items)
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return NotFound();

            ViewBag.AllStatuses = Enum.GetNames(typeof(PurchaseOrderStatus));
            ViewData["Title"] = po.PoNumber;
            return View(po);
        }

        // POST: /Admin/PurchaseOrders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, PurchaseOrderStatus newStatus)
        {
            var po = await _context.PurchaseOrders.FindAsync(id);
            if (po == null) return NotFound();

            var oldStatus = po.Status;
            po.Status = newStatus;
            po.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "PurchaseOrder", po.Id.ToString(), $"{po.PoNumber} status changed from {oldStatus} to {newStatus}");

            TempData["Success"] = $"{po.PoNumber} updated to {newStatus}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/PurchaseOrders/CreateInvoice/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvoice(int id, DateTime? dueDate)
        {
            var po = await _context.PurchaseOrders.Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == id);
            if (po == null) return NotFound();

            if (po.Invoice != null)
            {
                TempData["Error"] = "An invoice already exists for this purchase order.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var invoice = new Invoice
            {
                InvoiceNumber = "INV-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                PurchaseOrderId = po.Id,
                Amount = po.TotalAmount,
                Status = InvoiceStatus.Pending,
                IssuedDate = DateTime.UtcNow,
                DueDate = dueDate
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "Invoice", invoice.Id.ToString(), $"Created {invoice.InvoiceNumber} for {po.PoNumber}, amount {invoice.Amount:C}");

            TempData["Success"] = $"Invoice {invoice.InvoiceNumber} created.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/PurchaseOrders/MarkInvoicePaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInvoicePaid(int id)
        {
            var po = await _context.PurchaseOrders.Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == id);
            if (po?.Invoice == null) return NotFound();

            po.Invoice.Status = InvoiceStatus.Paid;
            po.Invoice.PaidDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Invoice", po.Invoice.Id.ToString(), $"Marked {po.Invoice.InvoiceNumber} as paid");

            TempData["Success"] = "Invoice marked as paid.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}