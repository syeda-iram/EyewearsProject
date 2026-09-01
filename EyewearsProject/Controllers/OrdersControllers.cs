using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EyewearsProject.Controllers
{
    // Every action here requires a logged-in customer.
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICartService _cartService;
        private readonly IInventoryService _inventoryService;

        public OrdersController(AppDbContext context, UserManager<ApplicationUser> userManager, ICartService cartService, IInventoryService inventoryService)
        {
            _context = context;
            _userManager = userManager;
            _cartService = cartService;
            _inventoryService = inventoryService;
        }

        // GET: /Orders
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: /Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Images)
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Security check: a customer must never view another customer's order
            if (order.UserId != userId)
            {
                return Forbid();
            }

            return View(order);
        }

        // GET: /Orders/Track/5
        public async Task<IActionResult> Track(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (order.UserId != userId) return Forbid();

            return View(order);
        }

        // Adds one order line back to the cart if it's still purchasable.
        // Returns null on success, or a message explaining why it was skipped.
        private async Task<string?> ReorderLineAsync(OrderItem item)
        {
            if (item.ProductVariantId == null)
                return $"{item.ProductName} is no longer available.";

            var available = await _inventoryService.GetAvailableQuantityAsync(item.ProductVariantId.Value);
            if (available <= 0)
                return $"{item.ProductName} is currently out of stock.";

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId.Value);

            if (variant == null)
                return $"{item.ProductName} is no longer available.";

            await _cartService.AddAsync(new CartItem
            {
                ProductId = item.ProductId,
                ProductVariantId = variant.Id,
                ProductName = item.ProductName,
                Color = variant.Color,
                ImageUrl = variant.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                           ?? variant.Product.Images.FirstOrDefault()?.ImageUrl,
                UnitPrice = variant.Product.DiscountPrice ?? variant.Product.Price,
                Quantity = Math.Min(item.Quantity, available),
                // Lens/coating aren't reordered automatically — prescriptions
                // can go out of date, so the customer re-selects those via
                // "Select Lenses" rather than silently reusing an old Rx.
            });

            return null;
        }

        // POST: /Orders/ReorderItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderItem(int orderId, int itemId)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.UserId != userId) return NotFound();

            var item = order.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return NotFound();

            var problem = await ReorderLineAsync(item);

            TempData[problem == null ? "Success" : "Error"] =
                problem ?? $"{item.ProductName} added to your cart.";

            return RedirectToAction("Details", new { id = orderId });
        }

        // POST: /Orders/ReorderAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderAll(int orderId)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.UserId != userId) return NotFound();

            int added = 0, skipped = 0;

            foreach (var item in order.Items)
            {
                var problem = await ReorderLineAsync(item);
                if (problem == null) added++;
                else skipped++;
            }

            TempData[added > 0 ? "Success" : "Error"] = skipped == 0
                ? $"Added {added} item(s) to your cart."
                : $"Added {added} item(s) to your cart. {skipped} item(s) are no longer available.";

            return RedirectToAction("Index", "Cart");
        }
    }
}