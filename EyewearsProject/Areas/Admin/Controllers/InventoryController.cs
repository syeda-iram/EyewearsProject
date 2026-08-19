using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.ProductsModule)]
    public class InventoryController : Controller
    {
        private const int LowStockThreshold = 10;
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public InventoryController(AppDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        // GET: /Admin/Inventory
        public async Task<IActionResult> Index(string? stockFilter, string? search)
        {
            var query = _context.ProductVariants
                .Include(v => v.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(v => v.Product.Name.Contains(search) || v.Sku.Contains(search));

            query = stockFilter switch
            {
                "out" => query.Where(v => v.StockQuantity == 0),
                "low" => query.Where(v => v.StockQuantity > 0 && v.StockQuantity <= LowStockThreshold),
                _ => query
            };

            var variants = await query.OrderBy(v => v.StockQuantity).ToListAsync();

            var list = variants.Select(v => new InventoryItemViewModel
            {
                VariantId = v.Id,
                ProductId = v.ProductId,
                ProductName = v.Product.Name,
                Sku = v.Sku,
                Color = v.Color,
                Size = v.Size,
                StockQuantity = v.StockQuantity
            }).ToList();

            ViewBag.LowStockThreshold = LowStockThreshold;
            ViewBag.TotalOutOfStock = await _context.ProductVariants.CountAsync(v => v.StockQuantity == 0);
            ViewBag.TotalLowStock = await _context.ProductVariants.CountAsync(v => v.StockQuantity > 0 && v.StockQuantity <= LowStockThreshold);
            ViewBag.CurrentFilter = stockFilter;
            ViewBag.CurrentSearch = search;
            ViewData["Title"] = "Inventory";
            return View(list);
        }

        // POST: /Admin/Inventory/AdjustStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int variantId, int stockQuantity, string? stockFilter, string? search)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) return NotFound();

            var oldQuantity = variant.StockQuantity;
            variant.StockQuantity = Math.Max(0, stockQuantity);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "ProductVariant", variant.Id.ToString(),
                $"Stock for {variant.Sku} changed from {oldQuantity} to {variant.StockQuantity}");

            TempData["Success"] = $"Stock updated for {variant.Sku}.";
            return RedirectToAction(nameof(Index), new { stockFilter, search });
        }
    }
}