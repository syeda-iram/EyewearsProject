using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.OrdersModule)]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;
        private readonly IInventoryService _inventoryService;

        public OrdersController(AppDbContext context, IAuditLogger auditLogger, IInventoryService inventoryService)
        {
            _context = context;
            _auditLogger = auditLogger;
            _inventoryService = inventoryService;
        }

        // GET: /Admin/Orders
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var parsedStatus))
                query = query.Where(o => o.OrderStatus == parsedStatus);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.OrderNumber.Contains(search) || o.User.Email!.Contains(search));

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            var list = orders.Select(o => new OrderListItemViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.User.FullName,
                CustomerEmail = o.User.Email ?? "",
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                GrandTotal = o.GrandTotal,
                ItemCount = o.Items.Count
            }).ToList();

            ViewBag.AllStatuses = Enum.GetNames(typeof(OrderStatus));
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.PaymentStatus == PaymentStatus.Paid)
                .SumAsync(o => (decimal?)o.GrandTotal) ?? 0;
            ViewData["Title"] = "Orders";
            return View(list);
        }

        // GET: /Admin/Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var model = new OrderDetailsViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.User.FullName,
                CustomerEmail = order.User.Email ?? "",
                OrderDate = order.OrderDate,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,

                RecipientName = order.RecipientName,
                RecipientEmail = order.RecipientEmail,
                RecipientPhone = order.RecipientPhone,

                ShippingAddressLine = order.ShippingAddressLine,
                ShippingCity = order.ShippingCity,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountry = order.ShippingCountry,

                BillingAddressLine = order.BillingAddressLine,
                BillingCity = order.BillingCity,
                BillingPostalCode = order.BillingPostalCode,
                BillingCountry = order.BillingCountry,

                PaymentMethod = order.PaymentMethod,
                TotalAmount = order.TotalAmount,
                DiscountAmount = order.DiscountAmount,
                ShippingAmount = order.ShippingAmount,
                TaxAmount = order.TaxAmount,
                GrandTotal = order.GrandTotal,
                Items = order.Items
            };

            ViewBag.AllStatuses = Enum.GetNames(typeof(OrderStatus));
            ViewData["Title"] = $"Order {order.OrderNumber}";
            return View(model);
        }

        // POST: /Admin/Orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            var oldStatus = order.OrderStatus;

            if (newStatus == OrderStatus.Delivered && oldStatus == OrderStatus.Cancelled)
            {
                TempData["Error"] = "A cancelled order cannot be marked as delivered.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.OrderStatus = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            // Cancelling releases the stock back — only once, guarded so re-saving the same status twice can't double-restore
            if (newStatus == OrderStatus.Cancelled && oldStatus != OrderStatus.Cancelled)
            {
                foreach (var item in order.Items)
                {
                    if (!item.ProductVariantId.HasValue) continue;

                    await _inventoryService.RecordTransactionAsync(
                        item.ProductVariantId.Value,
                        InventoryTransactionType.Return,
                        item.Quantity,
                        referenceType: "Order",
                        referenceId: order.Id.ToString(),
                        reason: $"Order {order.OrderNumber} cancelled — stock restored");
                }
            }

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Order", order.Id.ToString(),
                $"Order {order.OrderNumber} status changed from {oldStatus} to {newStatus}");

            TempData["Success"] = $"Order {order.OrderNumber} updated to {newStatus}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/Orders/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.OrderStatus == OrderStatus.Delivered)
            {
                TempData["Error"] = "A delivered order cannot be cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (order.OrderStatus == OrderStatus.Cancelled)
            {
                TempData["Error"] = "This order is already cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var oldStatus = order.OrderStatus;
            order.OrderStatus = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            foreach (var item in order.Items)
            {
                if (!item.ProductVariantId.HasValue) continue;

                await _inventoryService.RecordTransactionAsync(
                    item.ProductVariantId.Value,
                    InventoryTransactionType.Return,
                    item.Quantity,
                    referenceType: "Order",
                    referenceId: order.Id.ToString(),
                    reason: string.IsNullOrWhiteSpace(reason)
                        ? $"Order {order.OrderNumber} cancelled by admin — stock restored"
                        : $"Order {order.OrderNumber} cancelled: {reason}");
            }

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Order", order.Id.ToString(),
                $"Order {order.OrderNumber} cancelled (was {oldStatus}). Reason: {reason}");

            TempData["Success"] = $"Order {order.OrderNumber} cancelled and stock restored.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}