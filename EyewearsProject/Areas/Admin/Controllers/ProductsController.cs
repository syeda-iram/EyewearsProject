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
    [Authorize(Roles = AdminRoles.ProductsModule)]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public ProductsController(AppDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        private async Task PopulateDropdownsAsync()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            ViewBag.Brands = new SelectList(await _context.Brands.OrderBy(b => b.Name).ToListAsync(), "Id", "Name");
        }

        // GET: /Admin/Products
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewData["Title"] = "Products";
            return View(products);
        }

        // GET: /Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View(new ProductFormViewModel());
        }

        // POST: /Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                Sku = model.Sku,
                Description = model.Description,
                CategoryId = model.CategoryId,
                BrandId = model.BrandId,
                Price = model.Price,
                DiscountPrice = model.DiscountPrice,
                IsActive = model.IsActive
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "Product", product.Id.ToString(), $"Created product {product.Name} (SKU: {product.Sku})");

            TempData["Success"] = "Product created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var model = new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                Description = product.Description,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                IsActive = product.IsActive
            };

            await PopulateDropdownsAsync();
            return View(model);
        }

        // POST: /Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            if (id != model.Id) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            product.Name = model.Name;
            product.Sku = model.Sku;
            product.Description = model.Description;
            product.CategoryId = model.CategoryId;
            product.BrandId = model.BrandId;
            product.Price = model.Price;
            product.DiscountPrice = model.DiscountPrice;
            product.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Product", product.Id.ToString(), $"Updated product {product.Name}");

            TempData["Success"] = "Product updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Products/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            product.IsActive = !product.IsActive;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(product.IsActive ? "Activate" : "Deactivate", "Product", product.Id.ToString(), product.Name);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // log before removing — after Remove+SaveChanges, product.Name is gone
            await _auditLogger.LogAsync("Delete", "Product", product.Id.ToString(), product.Name);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Product deleted.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Products/Variants/5
        public async Task<IActionResult> Variants(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = product.Id;
            ViewData["Title"] = $"Variants — {product.Name}";
            return View(product.Variants.ToList());
        }

        // GET: /Admin/Products/CreateVariant?productId=5
        public async Task<IActionResult> CreateVariant(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            ViewBag.ProductName = product.Name;
            return View(new ProductVariantFormViewModel { ProductId = productId });
        }

        // POST: /Admin/Products/CreateVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVariant(ProductVariantFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                ViewBag.ProductName = product?.Name;
                return View(model);
            }

            var variant = new ProductVariant
            {
                ProductId = model.ProductId,
                Color = model.Color,
                Size = model.Size,
                Sku = model.Sku,
                StockQuantity = model.StockQuantity
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "ProductVariant", variant.Id.ToString(), $"Added variant {variant.Color} to product #{model.ProductId}");

            TempData["Success"] = "Variant added.";
            return RedirectToAction(nameof(Variants), new { id = model.ProductId });
        }

        // GET: /Admin/Products/EditVariant/5
        public async Task<IActionResult> EditVariant(int id)
        {
            var variant = await _context.ProductVariants.Include(v => v.Product).FirstOrDefaultAsync(v => v.Id == id);
            if (variant == null) return NotFound();

            ViewBag.ProductName = variant.Product.Name;
            var model = new ProductVariantFormViewModel
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                Color = variant.Color,
                Size = variant.Size,
                Sku = variant.Sku,
                StockQuantity = variant.StockQuantity
            };
            return View(model);
        }

        // POST: /Admin/Products/EditVariant/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVariant(int id, ProductVariantFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                ViewBag.ProductName = product?.Name;
                return View(model);
            }

            variant.Color = model.Color;
            variant.Size = model.Size;
            variant.Sku = model.Sku;
            variant.StockQuantity = model.StockQuantity;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "ProductVariant", variant.Id.ToString(), $"Updated variant {variant.Color}");

            TempData["Success"] = "Variant updated.";
            return RedirectToAction(nameof(Variants), new { id = model.ProductId });
        }

        // POST: /Admin/Products/DeleteVariant/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(int id, int productId)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null) return NotFound();

            await _auditLogger.LogAsync("Delete", "ProductVariant", variant.Id.ToString(), variant.Color);

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Variant deleted.";
            return RedirectToAction(nameof(Variants), new { id = productId });
        }

        // GET: /Admin/Products/Images/5
        public async Task<IActionResult> Images(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                    .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = product.Id;
            ViewData["Title"] = $"Images — {product.Name}";
            return View(product.Images.ToList());
        }

        // GET: /Admin/Products/CreateImage?productId=5
        public async Task<IActionResult> CreateImage(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            ViewBag.ProductName = product.Name;
            ViewBag.Variants = new SelectList(
                await _context.ProductVariants.Where(v => v.ProductId == productId).ToListAsync(),
                "Id", "Color");

            return View(new ProductImageFormViewModel { ProductId = productId });
        }

        // POST: /Admin/Products/CreateImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateImage(ProductImageFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                ViewBag.ProductName = product?.Name;
                ViewBag.Variants = new SelectList(
                    await _context.ProductVariants.Where(v => v.ProductId == model.ProductId).ToListAsync(),
                    "Id", "Color");
                return View(model);
            }

            if (model.IsPrimary)
            {
                var existingPrimaries = await _context.ProductImages
                    .Where(i => i.ProductId == model.ProductId && i.IsPrimary)
                    .ToListAsync();
                foreach (var img in existingPrimaries) img.IsPrimary = false;
            }

            var image = new ProductImage
            {
                ProductId = model.ProductId,
                ImageUrl = model.ImageUrl,
                IsPrimary = model.IsPrimary,
                ProductVariantId = model.ProductVariantId
            };

            _context.ProductImages.Add(image);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "ProductImage", image.Id.ToString(), $"Added image to product #{model.ProductId}");

            TempData["Success"] = "Image added.";
            return RedirectToAction(nameof(Images), new { id = model.ProductId });
        }

        // POST: /Admin/Products/SetPrimaryImage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryImage(int id, int productId)
        {
            var images = await _context.ProductImages.Where(i => i.ProductId == productId).ToListAsync();
            foreach (var img in images) img.IsPrimary = (img.Id == id);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Images), new { id = productId });
        }

        // POST: /Admin/Products/DeleteImage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int productId)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null) return NotFound();

            await _auditLogger.LogAsync("Delete", "ProductImage", image.Id.ToString(), image.ImageUrl);

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Image deleted.";
            return RedirectToAction(nameof(Images), new { id = productId });
        }
    }
}