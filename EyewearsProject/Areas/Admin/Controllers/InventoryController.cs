using System.Text;
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

        public InventoryController(
            AppDbContext context,
            IAuditLogger auditLogger,
            IInventoryService inventoryService)
        {
            _context = context;
            _auditLogger = auditLogger;
            _inventoryService = inventoryService;
        }

        // GET: /Admin/Inventory
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            int? brandId,
            string? stockStatus,
            int page = 1)
        {
            const int pageSize = 10;

            // Make sure every product variant has an inventory row
            var variantIds = await _context.ProductVariants
                .Select(v => v.Id)
                .ToListAsync();

            var existingInventoryVariantIds = await _context.Inventories
                .Select(i => i.ProductVariantId)
                .ToListAsync();

            foreach (var id in variantIds.Except(existingInventoryVariantIds))
            {
                await _inventoryService.GetOrCreateAsync(id);
            }

            var query = _context.Inventories
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Category)
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Brand)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(i =>
                    i.ProductVariant.Product.Name.Contains(search) ||
                    i.ProductVariant.Sku.Contains(search) ||
                    i.ProductVariant.Product.Sku.Contains(search));
            }

            // Category
            if (categoryId.HasValue)
            {
                query = query.Where(i =>
                    i.ProductVariant.Product.CategoryId == categoryId.Value);
            }

            // Brand
            if (brandId.HasValue)
            {
                query = query.Where(i =>
                    i.ProductVariant.Product.BrandId == brandId.Value);
            }

            // Stock Status
            query = stockStatus switch
            {
                "out" => query.Where(i =>
                    i.QuantityOnHand - i.ReservedQuantity <= 0),

                "low" => query.Where(i =>
                    i.QuantityOnHand - i.ReservedQuantity > 0 &&
                    i.QuantityOnHand - i.ReservedQuantity <= i.ReorderLevel),

                "in" => query.Where(i =>
                    i.QuantityOnHand - i.ReservedQuantity > i.ReorderLevel),

                _ => query
            };

            var totalItems = await query.CountAsync();

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalItems / (double)pageSize));

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var inventories = await query
                .OrderBy(i => i.ProductVariant.Product.Name)
                .ThenBy(i => i.ProductVariant.Color)
                .ThenBy(i => i.ProductVariant.Size)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var list = inventories.Select(i => new InventoryItemViewModel
            {
                InventoryId = i.Id,
                VariantId = i.ProductVariantId,
                ProductId = i.ProductVariant.ProductId,

                ProductName = i.ProductVariant.Product.Name,
                Sku = i.ProductVariant.Sku,

                CategoryName = i.ProductVariant.Product.Category.Name,
                BrandName = i.ProductVariant.Product.Brand.Name,

                Color = i.ProductVariant.Color,
                Size = i.ProductVariant.Size,

                QuantityOnHand = i.QuantityOnHand,
                ReservedQuantity = i.ReservedQuantity,
                AvailableQuantity = i.AvailableQuantity,
                ReorderLevel = i.ReorderLevel,

                UnitPrice = i.ProductVariant.Product.DiscountPrice
                    ?? i.ProductVariant.Product.Price

            }).ToList();

            // Dashboard cards
            ViewBag.TotalSKUs = await _context.Inventories.CountAsync();

            ViewBag.TotalOutOfStock =
                await _context.Inventories.CountAsync(i =>
                    i.QuantityOnHand - i.ReservedQuantity <= 0);

            ViewBag.TotalLowStock =
                await _context.Inventories.CountAsync(i =>
                    i.QuantityOnHand - i.ReservedQuantity > 0 &&
                    i.QuantityOnHand - i.ReservedQuantity <= i.ReorderLevel);

            ViewBag.TotalStockValue =
                await _context.Inventories
                    .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                    .SumAsync(i =>
                        i.QuantityOnHand *
                        (i.ProductVariant.Product.DiscountPrice
                         ?? i.ProductVariant.Product.Price));

            // Dropdowns
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Brands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentBrand = brandId;
            ViewBag.CurrentStockStatus = stockStatus;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            ViewData["Title"] = "Inventory";

            return View(list);
        }

        // POST: /Admin/Inventory/AdjustStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(
            int variantId,
            int newQuantityOnHand,
            string? reason,
            string? search,
            int? categoryId,
            int? brandId,
            string? stockStatus,
            int page = 1)
        {
            var variant = await _context.ProductVariants
                .FindAsync(variantId);

            if (variant == null)
                return NotFound();

            var transaction = await _inventoryService.AdjustToQuantityAsync(
                variantId,
                Math.Max(0, newQuantityOnHand),
                string.IsNullOrWhiteSpace(reason)
                    ? "Manual stock adjustment by admin"
                    : reason);

            if (transaction != null)
            {
                await _auditLogger.LogAsync(
                    "Update",
                    "Inventory",
                    variantId.ToString(),
                    $"Stock for {variant.Sku} adjusted by " +
                    $"{transaction.Quantity:+#;-#;0} " +
                    $"(reason: {transaction.Reason})");
            }

            TempData["Success"] =
                $"Stock updated for {variant.Sku}.";

            return RedirectToAction(nameof(Index), new
            {
                search,
                categoryId,
                brandId,
                stockStatus,
                page
            });
        }

        // GET: /Admin/Inventory/ExportCsv
        [HttpGet]
        public async Task<IActionResult> ExportCsv(
            string? search,
            int? categoryId,
            int? brandId,
            string? stockStatus)
        {
            var query = _context.Inventories
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Category)
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Brand)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(i =>
                    i.ProductVariant.Product.Name.Contains(search) ||
                    i.ProductVariant.Sku.Contains(search) ||
                    i.ProductVariant.Product.Sku.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(i =>
                    i.ProductVariant.Product.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(i =>
                    i.ProductVariant.Product.BrandId == brandId.Value);
            }

            query = stockStatus switch
            {
                "out" => query.Where(i =>
                    i.QuantityOnHand - i.ReservedQuantity <= 0),

                "low" => query.Where(i =>
                    i.QuantityOnHand - i.ReservedQuantity > 0 &&
                    i.QuantityOnHand - i.ReservedQuantity <= i.ReorderLevel),

                "in" => query.Where(i =>
                    i.QuantityOnHand - i.ReservedQuantity > i.ReorderLevel),

                _ => query
            };

            var items = await query
                .OrderBy(i => i.ProductVariant.Product.Name)
                .ThenBy(i => i.ProductVariant.Color)
                .ToListAsync();

            var csv = new StringBuilder();

            csv.AppendLine(
                "Product,SKU,Category,Brand,Variant,Quantity On Hand,Reserved,Available,Reorder Level,Status,Unit Price,Stock Value");

            foreach (var i in items)
            {
                var available = i.QuantityOnHand - i.ReservedQuantity;

                var status =
                    available <= 0
                        ? "Out of Stock"
                        : available <= i.ReorderLevel
                            ? "Low Stock"
                            : "In Stock";

                var unitPrice =
                    i.ProductVariant.Product.DiscountPrice
                    ?? i.ProductVariant.Product.Price;

                var stockValue =
                    i.QuantityOnHand * unitPrice;

                csv.AppendLine(string.Join(",",
                    Csv(i.ProductVariant.Product.Name),
                    Csv(i.ProductVariant.Sku),
                    Csv(i.ProductVariant.Product.Category.Name),
                    Csv(i.ProductVariant.Product.Brand.Name),
                    Csv($"{i.ProductVariant.Color} {(i.ProductVariant.Size ?? "")}".Trim()),
                    i.QuantityOnHand,
                    i.ReservedQuantity,
                    available,
                    i.ReorderLevel,
                    Csv(status),
                    unitPrice.ToString("0.00"),
                    stockValue.ToString("0.00")
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(
                bytes,
                "text/csv",
                $"inventory-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }

        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            value = value.Replace("\"", "\"\"");

            return $"\"{value}\"";
        }

        // GET: /Admin/Inventory/History/5
        public async Task<IActionResult> History(int variantId)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == variantId);

            if (variant == null)
                return NotFound();

            var transactions = await _context.InventoryTransactions
                .Where(t => t.ProductVariantId == variantId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.ProductName = variant.Product.Name;

            ViewBag.VariantLabel =
                $"{variant.Color} " +
                $"{(variant.Size != null ? "- " + variant.Size : "")} " +
                $"({variant.Sku})";

            ViewData["Title"] =
                $"Stock History — {variant.Product.Name}";

            return View(transactions);
        }
    }
}