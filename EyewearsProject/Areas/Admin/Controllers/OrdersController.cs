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

        // Checks the actual transaction ledger — not just order status — so a double-click
        // or repeated status save can never create a duplicate Sale/Release for the same order+variant.
        private async Task<bool> HasTransactionAsync(int orderId, int variantId, InventoryTransactionType type)
        {
            return await _context.InventoryTransactions.AnyAsync(t =>
                t.ReferenceType == "Order" &&
                t.ReferenceId == orderId.ToString() &&
                t.ProductVariantId == variantId &&
                t.TransactionType == type);
        }

        // Converts a reserved order into a finalized sale (stock actually leaves the shelf).
        // Safe to call more than once — skips items that already have a Sale transaction.
        private async Task ConvertReservationToSaleAsync(Order order)
        {
            foreach (var item in order.Items)
            {
                if (item.ProductVariantId == null) continue;

                if (await HasTransactionAsync(order.Id, item.ProductVariantId.Value, InventoryTransactionType.Sale))
                    continue; // already converted — don't double-deduct

                await _inventoryService.RecordTransactionAsync(
                    item.ProductVariantId.Value,
                    InventoryTransactionType.Sale,
                    item.Quantity,
                    referenceType: "Order",
                    referenceId: order.Id.ToString(),
                    reason: $"Order {order.OrderNumber} shipped — reservation converted to sale");
            }
        }

        // Releases a reservation without touching QuantityOnHand — used for cancellation
        // BEFORE the order has shipped. If it already shipped, stock was already sold and
        // must be restored via Return instead (handled separately, not by this method).
        private async Task ReleaseReservationAsync(Order order, string reason)
        {
            foreach (var item in order.Items)
            {
                if (item.ProductVariantId == null) continue;

                if (await HasTransactionAsync(order.Id, item.ProductVariantId.Value, InventoryTransactionType.Release))
                    continue; // already released — don't double-release

                await _inventoryService.RecordTransactionAsync(
                    item.ProductVariantId.Value,
                    InventoryTransactionType.Release,
                    item.Quantity,
                    referenceType: "Order",
                    referenceId: order.Id.ToString(),
                    reason: reason);
            }
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

            // Pending -> Processing: no inventory movement, reservation already exists from checkout.

            // Processing -> Shipped: reservation converts into a real sale (stock actually leaves the shelf).
            if (newStatus == OrderStatus.Shipped && oldStatus != OrderStatus.Shipped)
            {
                await ConvertReservationToSaleAsync(order);
            }

            // -> Cancelled: release the reservation. If it already shipped (already sold),
            // that's a real return of physical stock instead — handled by the dedicated Cancel action's check.
            if (newStatus == OrderStatus.Cancelled && oldStatus != OrderStatus.Cancelled)
            {
                if (oldStatus == OrderStatus.Shipped || oldStatus == OrderStatus.Delivered)
                {
                    foreach (var item in order.Items)
                    {
                        if (item.ProductVariantId == null) continue;
                        await _inventoryService.RecordTransactionAsync(
                            item.ProductVariantId.Value,
                            InventoryTransactionType.Return,
                            item.Quantity,
                            referenceType: "Order",
                            referenceId: order.Id.ToString(),
                            reason: $"Order {order.OrderNumber} cancelled after shipping — stock returned");
                    }
                }
                else
                {
                    await ReleaseReservationAsync(order, $"Order {order.OrderNumber} cancelled — reservation released");
                }
            }

            // Delivered: no new stock transaction — stock was already finalized at Shipped.
            order.OrderStatus = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = newStatus,
                ChangedAt = order.UpdatedAt.Value
            });

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

            if (oldStatus == OrderStatus.Shipped)
            {
                // Stock already left the shelf at Shipped — cancelling now is a real physical return.
                foreach (var item in order.Items)
                {
                    if (item.ProductVariantId == null) continue;
                    await _inventoryService.RecordTransactionAsync(
                        item.ProductVariantId.Value,
                        InventoryTransactionType.Return,
                        item.Quantity,
                        referenceType: "Order",
                        referenceId: order.Id.ToString(),
                        reason: string.IsNullOrWhiteSpace(reason)
                            ? $"Order {order.OrderNumber} cancelled after shipping — stock returned"
                            : $"Order {order.OrderNumber} cancelled after shipping: {reason}");
                }
            }
            else
            {
                // Still Pending/Processing — nothing was ever removed from the shelf, just release the hold.
                await ReleaseReservationAsync(order,
                    string.IsNullOrWhiteSpace(reason)
                        ? $"Order {order.OrderNumber} cancelled by admin — reservation released"
                        : $"Order {order.OrderNumber} cancelled: {reason}");
            }

            order.OrderStatus = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = OrderStatus.Cancelled,
                ChangedAt = order.UpdatedAt.Value,
                Note = reason
            });

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Order", order.Id.ToString(),
                $"Order {order.OrderNumber} cancelled (was {oldStatus}). Reason: {reason}");
            TempData["Success"] = $"Order {order.OrderNumber} cancelled and inventory adjusted.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}