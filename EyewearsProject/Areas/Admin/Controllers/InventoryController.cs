using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.InventoryModule)]
    public class InventoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;
        private readonly IInventoryService _inventoryService;

        public InventoryController(AppDbContext context, IAuditLogger auditLogger, IInventoryService inventoryService)
        {
            _context = context;
            _auditLogger = auditLogger;
            _inventoryService = inventoryService;
        }

        // GET: /Admin/Inventory
        public async Task<IActionResult> Index(string? stockFilter, string? search)
        {
            // Make sure every variant has a real Inventory row (auto-migrates legacy StockQuantity on first touch)
            var variantIds = await _context.ProductVariants.Select(v => v.Id).ToListAsync();
            var existingInventoryVariantIds = await _context.Inventories.Select(i => i.ProductVariantId).ToListAsync();
            foreach (var id in variantIds.Except(existingInventoryVariantIds))
                await _inventoryService.GetOrCreateAsync(id);

            var query = _context.Inventories
                .Include(i => i.ProductVariant).ThenInclude(v => v.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.ProductVariant.Product.Name.Contains(search) || i.ProductVariant.Sku.Contains(search));

            query = stockFilter switch
            {
                "out" => query.Where(i => i.QuantityOnHand - i.ReservedQuantity <= 0),
                "low" => query.Where(i => i.QuantityOnHand - i.ReservedQuantity > 0 && i.QuantityOnHand - i.ReservedQuantity <= i.ReorderLevel),
                _ => query
            };

            var inventories = await query.OrderBy(i => i.QuantityOnHand - i.ReservedQuantity).ToListAsync();

            var list = inventories.Select(i => new InventoryItemViewModel
            {
                InventoryId = i.Id,
                VariantId = i.ProductVariantId,
                ProductId = i.ProductVariant.ProductId,
                ProductName = i.ProductVariant.Product.Name,
                Sku = i.ProductVariant.Sku,
                Color = i.ProductVariant.Color,
                Size = i.ProductVariant.Size,
                QuantityOnHand = i.QuantityOnHand,
                ReservedQuantity = i.ReservedQuantity,
                AvailableQuantity = i.AvailableQuantity,
                ReorderLevel = i.ReorderLevel
            }).ToList();

            ViewBag.TotalOutOfStock = await _context.Inventories.CountAsync(i => i.QuantityOnHand - i.ReservedQuantity <= 0);
            ViewBag.TotalLowStock = await _context.Inventories.CountAsync(i => i.QuantityOnHand - i.ReservedQuantity > 0 && i.QuantityOnHand - i.ReservedQuantity <= i.ReorderLevel);
            ViewBag.CurrentFilter = stockFilter;
            ViewBag.CurrentSearch = search;
            ViewData["Title"] = "Inventory";
            return View(list);
        }

        // POST: /Admin/Inventory/AdjustStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int variantId, int newQuantityOnHand, string? reason, string? stockFilter, string? search)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) return NotFound();

            var transaction = await _inventoryService.AdjustToQuantityAsync(
                variantId,
                Math.Max(0, newQuantityOnHand),
                string.IsNullOrWhiteSpace(reason) ? "Manual stock adjustment by admin" : reason);

            if (transaction != null)
            {
                await _auditLogger.LogAsync("Update", "Inventory", variantId.ToString(),
                    $"Stock for {variant.Sku} adjusted by {transaction.Quantity:+#;-#;0} (reason: {transaction.Reason})");
            }

            TempData["Success"] = $"Stock updated for {variant.Sku}.";
            return RedirectToAction(nameof(Index), new { stockFilter, search });
        }

        // GET: /Admin/Inventory/History/5  (variantId)
        public async Task<IActionResult> History(int variantId)
        {
            var variant = await _context.ProductVariants.Include(v => v.Product).FirstOrDefaultAsync(v => v.Id == variantId);
            if (variant == null) return NotFound();

            var transactions = await _context.InventoryTransactions
                .Where(t => t.ProductVariantId == variantId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.ProductName = variant.Product.Name;
            ViewBag.VariantLabel = $"{variant.Color} {(variant.Size != null ? "- " + variant.Size : "")} ({variant.Sku})";
            ViewData["Title"] = $"Stock History — {variant.Product.Name}";
            return View(transactions);
        }
    }
}