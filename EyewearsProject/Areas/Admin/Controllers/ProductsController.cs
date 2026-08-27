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
        private readonly IInventoryService _inventoryService;

        public ProductsController(
            AppDbContext context,
            IAuditLogger auditLogger,
            IInventoryService inventoryService)
        {
            _context = context;
            _auditLogger = auditLogger;
            _inventoryService = inventoryService;
        }

        private async Task PopulateDropdownsAsync()
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Id",
                "Name");

            ViewBag.Brands = new SelectList(
                await _context.Brands
                    .OrderBy(b => b.Name)
                    .ToListAsync(),
                "Id",
                "Name");
        }

        // =========================================================
        // PRODUCTS
        // =========================================================

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

                ShortDescription = model.ShortDescription,
                Description = model.Description,

                ProductType = model.ProductType,
                Gender = model.Gender,
                Material = model.Material,
                Shape = model.Shape,
                Color = model.Color,

                CategoryId = model.CategoryId,
                BrandId = model.BrandId,

                Price = model.Price,
                DiscountPrice = model.DiscountPrice,
                CostPrice = model.CostPrice,
                Weight = model.Weight,

                IsFeatured = model.IsFeatured,
                IsActive = model.IsActive,

                TryOnOverlayImageUrl = model.TryOnOverlayImageUrl,
                TryOn3DModelUrl = model.TryOn3DModelUrl,
                TryOnOverlayScale = model.TryOnOverlayScale,
                TryOnOverlayVerticalOffset = model.TryOnOverlayVerticalOffset,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Create",
                "Product",
                product.Id.ToString(),
                $"Created product {product.Name} (SKU: {product.Sku})");

            TempData["Success"] = "Product created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            var model = new ProductFormViewModel
            {
                Id = product.Id,

                Name = product.Name,
                Sku = product.Sku,

                ShortDescription = product.ShortDescription,
                Description = product.Description,

                ProductType = product.ProductType,
                Gender = product.Gender,
                Material = product.Material,
                Shape = product.Shape,
                Color = product.Color,

                CategoryId = product.CategoryId,
                BrandId = product.BrandId,

                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                CostPrice = product.CostPrice,
                Weight = product.Weight,

                IsFeatured = product.IsFeatured,
                IsActive = product.IsActive,

                TryOnOverlayImageUrl = product.TryOnOverlayImageUrl,
                TryOn3DModelUrl = product.TryOn3DModelUrl,
                TryOnOverlayScale = product.TryOnOverlayScale,
                TryOnOverlayVerticalOffset = product.TryOnOverlayVerticalOffset
            };

            await PopulateDropdownsAsync();

            return View(model);
        }

        // POST: /Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ProductFormViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            product.Name = model.Name;
            product.Sku = model.Sku;

            product.ShortDescription = model.ShortDescription;
            product.Description = model.Description;

            product.ProductType = model.ProductType;
            product.Gender = model.Gender;
            product.Material = model.Material;
            product.Shape = model.Shape;
            product.Color = model.Color;

            product.CategoryId = model.CategoryId;
            product.BrandId = model.BrandId;

            product.Price = model.Price;
            product.DiscountPrice = model.DiscountPrice;
            product.CostPrice = model.CostPrice;
            product.Weight = model.Weight;

            product.IsFeatured = model.IsFeatured;
            product.IsActive = model.IsActive;

            product.TryOnOverlayImageUrl = model.TryOnOverlayImageUrl;
            product.TryOn3DModelUrl = model.TryOn3DModelUrl;
            product.TryOnOverlayScale = model.TryOnOverlayScale;
            product.TryOnOverlayVerticalOffset =
                model.TryOnOverlayVerticalOffset;

            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Update",
                "Product",
                product.Id.ToString(),
                $"Updated product {product.Name}");

            TempData["Success"] = "Product updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Products/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            product.IsActive = !product.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                product.IsActive ? "Activate" : "Deactivate",
                "Product",
                product.Id.ToString(),
                product.Name);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            await _auditLogger.LogAsync(
                "Delete",
                "Product",
                product.Id.ToString(),
                product.Name);

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Product deleted.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // VARIANTS
        // =========================================================

        // GET: /Admin/Products/Variants/5
        public async Task<IActionResult> Variants(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = product.Id;

            ViewData["Title"] = $"Variants — {product.Name}";

            return View(product.Variants.ToList());
        }

        // GET: /Admin/Products/CreateVariant?productId=5
        public async Task<IActionResult> CreateVariant(int productId)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
                return NotFound();

            ViewBag.ProductName = product.Name;

            return View(
                new ProductVariantFormViewModel
                {
                    ProductId = productId
                });
        }

        // POST: /Admin/Products/CreateVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVariant(
            ProductVariantFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var product =
                    await _context.Products.FindAsync(model.ProductId);

                ViewBag.ProductName = product?.Name;

                return View(model);
            }

            var variant = new ProductVariant
            {
                ProductId = model.ProductId,
                Color = model.Color,
                Size = model.Size,
                Sku = model.Sku,
                StockQuantity = 0
            };

            _context.ProductVariants.Add(variant);

            await _context.SaveChangesAsync();

            if (model.StockQuantity > 0)
            {
                await _inventoryService.RecordTransactionAsync(
                    variant.Id,
                    InventoryTransactionType.Purchase,
                    model.StockQuantity,
                    referenceType: "ProductVariantCreate",
                    reason: $"Initial stock set while creating variant {variant.Color}");
            }

            await _auditLogger.LogAsync(
                "Create",
                "ProductVariant",
                variant.Id.ToString(),
                $"Added variant {variant.Color} to product #{model.ProductId}");

            TempData["Success"] = "Variant added.";

            return RedirectToAction(
                nameof(Variants),
                new { id = model.ProductId });
        }

        // GET: /Admin/Products/EditVariant/5
        public async Task<IActionResult> EditVariant(int id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variant == null)
                return NotFound();

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
        public async Task<IActionResult> EditVariant(
            int id,
            ProductVariantFormViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var variant =
                await _context.ProductVariants.FindAsync(id);

            if (variant == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var product =
                    await _context.Products.FindAsync(model.ProductId);

                ViewBag.ProductName = product?.Name;

                return View(model);
            }

            variant.Color = model.Color;
            variant.Size = model.Size;
            variant.Sku = model.Sku;

            await _context.SaveChangesAsync();

            await _inventoryService.AdjustToQuantityAsync(
                variant.Id,
                model.StockQuantity,
                reason: $"Stock updated via product variant edit form ({variant.Color})");

            await _auditLogger.LogAsync(
                "Update",
                "ProductVariant",
                variant.Id.ToString(),
                $"Updated variant {variant.Color}");

            TempData["Success"] = "Variant updated.";

            return RedirectToAction(
                nameof(Variants),
                new { id = model.ProductId });
        }

        // POST: /Admin/Products/DeleteVariant/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(
            int id,
            int productId)
        {
            var variant =
                await _context.ProductVariants.FindAsync(id);

            if (variant == null)
                return NotFound();

            await _auditLogger.LogAsync(
                "Delete",
                "ProductVariant",
                variant.Id.ToString(),
                variant.Color);

            _context.ProductVariants.Remove(variant);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Variant deleted.";

            return RedirectToAction(
                nameof(Variants),
                new { id = productId });
        }

        // =========================================================
        // IMAGES
        // =========================================================

        // GET: /Admin/Products/Images/5
        public async Task<IActionResult> Images(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = product.Id;

            ViewData["Title"] = $"Images — {product.Name}";

            return View(product.Images.ToList());
        }

        // GET: /Admin/Products/CreateImage?productId=5
        public async Task<IActionResult> CreateImage(int productId)
        {
            var product =
                await _context.Products.FindAsync(productId);

            if (product == null)
                return NotFound();

            ViewBag.ProductName = product.Name;

            ViewBag.Variants = new SelectList(
                await _context.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .ToListAsync(),
                "Id",
                "Color");

            return View(
                new ProductImageFormViewModel
                {
                    ProductId = productId
                });
        }

        // POST: /Admin/Products/CreateImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateImage(
            ProductImageFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var product =
                    await _context.Products.FindAsync(model.ProductId);

                ViewBag.ProductName = product?.Name;

                ViewBag.Variants = new SelectList(
                    await _context.ProductVariants
                        .Where(v => v.ProductId == model.ProductId)
                        .ToListAsync(),
                    "Id",
                    "Color");

                return View(model);
            }

            if (model.IsPrimary)
            {
                var existingPrimaries =
                    await _context.ProductImages
                        .Where(i =>
                            i.ProductId == model.ProductId &&
                            i.IsPrimary)
                        .ToListAsync();

                foreach (var img in existingPrimaries)
                    img.IsPrimary = false;
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

            await _auditLogger.LogAsync(
                "Create",
                "ProductImage",
                image.Id.ToString(),
                $"Added image to product #{model.ProductId}");

            TempData["Success"] = "Image added.";

            return RedirectToAction(
                nameof(Images),
                new { id = model.ProductId });
        }

        // POST: /Admin/Products/SetPrimaryImage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryImage(
            int id,
            int productId)
        {
            var images =
                await _context.ProductImages
                    .Where(i => i.ProductId == productId)
                    .ToListAsync();

            foreach (var img in images)
                img.IsPrimary = img.Id == id;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Images),
                new { id = productId });
        }

        // POST: /Admin/Products/DeleteImage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(
            int id,
            int productId)
        {
            var image =
                await _context.ProductImages.FindAsync(id);

            if (image == null)
                return NotFound();

            await _auditLogger.LogAsync(
                "Delete",
                "ProductImage",
                image.Id.ToString(),
                image.ImageUrl);

            _context.ProductImages.Remove(image);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Image deleted.";

            return RedirectToAction(
                nameof(Images),
                new { id = productId });
        }

        // =========================================================
        // SPECIFICATIONS
        // =========================================================

        // GET: /Admin/Products/Specifications/5
        public async Task<IActionResult> Specifications(int id)
        {
            var product = await _context.Products
                .Include(p => p.Specifications)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = product.Id;

            ViewData["Title"] =
                $"Specifications — {product.Name}";

            return View(
                product.Specifications
                    .OrderBy(s => s.SortOrder)
                    .ToList());
        }

        // GET: /Admin/Products/CreateSpecification?productId=5
        public async Task<IActionResult> CreateSpecification(
            int productId)
        {
            var product =
                await _context.Products.FindAsync(productId);

            if (product == null)
                return NotFound();

            ViewBag.ProductName = product.Name;

            return View(
                new ProductSpecificationFormViewModel
                {
                    ProductId = productId
                });
        }

        // POST: /Admin/Products/CreateSpecification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSpecification(
            ProductSpecificationFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var product =
                    await _context.Products.FindAsync(model.ProductId);

                ViewBag.ProductName = product?.Name;

                return View(model);
            }

            var spec = new ProductSpecification
            {
                ProductId = model.ProductId,
                Name = model.Name,
                Value = model.Value,
                SortOrder = model.SortOrder
            };

            _context.ProductSpecifications.Add(spec);

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Create",
                "ProductSpecification",
                spec.Id.ToString(),
                $"Added spec '{spec.Name}: {spec.Value}' to product #{model.ProductId}");

            TempData["Success"] = "Specification added.";

            return RedirectToAction(
                nameof(Specifications),
                new { id = model.ProductId });
        }

        // GET: /Admin/Products/EditSpecification/5
        public async Task<IActionResult> EditSpecification(int id)
        {
            var spec = await _context.ProductSpecifications
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (spec == null)
                return NotFound();

            ViewBag.ProductName = spec.Product.Name;

            var model = new ProductSpecificationFormViewModel
            {
                Id = spec.Id,
                ProductId = spec.ProductId,
                Name = spec.Name,
                Value = spec.Value,
                SortOrder = spec.SortOrder
            };

            return View(model);
        }

        // POST: /Admin/Products/EditSpecification/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpecification(
            int id,
            ProductSpecificationFormViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var spec =
                await _context.ProductSpecifications.FindAsync(id);

            if (spec == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var product =
                    await _context.Products.FindAsync(model.ProductId);

                ViewBag.ProductName = product?.Name;

                return View(model);
            }

            spec.Name = model.Name;
            spec.Value = model.Value;
            spec.SortOrder = model.SortOrder;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Update",
                "ProductSpecification",
                spec.Id.ToString(),
                $"Updated spec '{spec.Name}'");

            TempData["Success"] = "Specification updated.";

            return RedirectToAction(
                nameof(Specifications),
                new { id = model.ProductId });
        }

        // POST: /Admin/Products/DeleteSpecification/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpecification(
            int id,
            int productId)
        {
            var spec =
                await _context.ProductSpecifications.FindAsync(id);

            if (spec == null)
                return NotFound();

            await _auditLogger.LogAsync(
                "Delete",
                "ProductSpecification",
                spec.Id.ToString(),
                spec.Name);

            _context.ProductSpecifications.Remove(spec);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Specification deleted.";

            return RedirectToAction(
                nameof(Specifications),
                new { id = productId });
        }

        // =========================================================
        // ATTRIBUTES
        // =========================================================

        // GET: /Admin/Products/Attributes/5
        public async Task<IActionResult> Attributes(int id)
        {
            var product = await _context.Products
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = product.Id;
            ViewData["Title"] = $"Attributes — {product.Name}";

            return View(
                product.Attributes
                    .OrderBy(a => a.SortOrder)
                    .ToList());
        }

        // GET: /Admin/Products/CreateAttribute?productId=5
        public async Task<IActionResult> CreateAttribute(int productId)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
                return NotFound();

            ViewBag.ProductName = product.Name;

            return View(new ProductAttributeFormViewModel
            {
                ProductId = productId
            });
        }

        // POST: /Admin/Products/CreateAttribute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAttribute(
            ProductAttributeFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                ViewBag.ProductName = product?.Name;

                return View(model);
            }

            var attribute = new ProductAttribute
            {
                ProductId = model.ProductId,
                Name = model.Name,
                Value = model.Value,
                SortOrder = model.SortOrder
            };

            _context.ProductAttributes.Add(attribute);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Create",
                "ProductAttribute",
                attribute.Id.ToString(),
                $"Added attribute '{attribute.Name}: {attribute.Value}' to product #{model.ProductId}");

            TempData["Success"] = "Attribute added.";

            return RedirectToAction(
                nameof(Attributes),
                new { id = model.ProductId });
        }

        // GET: /Admin/Products/EditAttribute/5
        public async Task<IActionResult> EditAttribute(int id)
        {
            var attribute = await _context.ProductAttributes
                .Include(a => a.Product)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attribute == null)
                return NotFound();

            ViewBag.ProductName = attribute.Product.Name;

            var model = new ProductAttributeFormViewModel
            {
                Id = attribute.Id,
                ProductId = attribute.ProductId,
                Name = attribute.Name,
                Value = attribute.Value,
                SortOrder = attribute.SortOrder
            };

            return View(model);
        }

        // POST: /Admin/Products/EditAttribute/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAttribute(
            int id,
            ProductAttributeFormViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var attribute = await _context.ProductAttributes
                .FindAsync(id);

            if (attribute == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var product = await _context.Products
                    .FindAsync(model.ProductId);

                ViewBag.ProductName = product?.Name;

                return View(model);
            }

            attribute.Name = model.Name;
            attribute.Value = model.Value;
            attribute.SortOrder = model.SortOrder;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Update",
                "ProductAttribute",
                attribute.Id.ToString(),
                $"Updated attribute '{attribute.Name}'");

            TempData["Success"] = "Attribute updated.";

            return RedirectToAction(
                nameof(Attributes),
                new { id = model.ProductId });
        }

        // POST: /Admin/Products/DeleteAttribute/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttribute(
            int id,
            int productId)
        {
            var attribute = await _context.ProductAttributes
                .FindAsync(id);

            if (attribute == null)
                return NotFound();

            await _auditLogger.LogAsync(
                "Delete",
                "ProductAttribute",
                attribute.Id.ToString(),
                attribute.Name);

            _context.ProductAttributes.Remove(attribute);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Attribute deleted.";

            return RedirectToAction(
                nameof(Attributes),
                new { id = productId });
        }

        // =========================================================
        // TAGS
        // =========================================================

        // GET: /Admin/Products/Tags/5
        public async Task<IActionResult> Tags(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductTags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = product.Id;
            ViewData["Title"] = $"Tags — {product.Name}";

            return View(product.ProductTags.OrderBy(t => t.Name).ToList());
        }

        // GET: /Admin/Products/CreateTag?productId=5
        public async Task<IActionResult> CreateTag(int productId)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null) return NotFound();

            ViewBag.ProductName = product.Name;

            return View(new ProductTagFormViewModel
            {
                ProductId = productId
            });
        }

        // POST: /Admin/Products/CreateTag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTag(ProductTagFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                ViewBag.ProductName = product?.Name;

                return View(model);
            }

            var tag = new ProductTag
            {
                ProductId = model.ProductId,
                Name = model.Name.Trim()
            };

            _context.ProductTags.Add(tag);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Create",
                "ProductTag",
                tag.Id.ToString(),
                $"Added tag '{tag.Name}' to product #{model.ProductId}");

            TempData["Success"] = "Tag added.";

            return RedirectToAction(
                nameof(Tags),
                new { id = model.ProductId });
        }

        // GET: /Admin/Products/EditTag/5
        public async Task<IActionResult> EditTag(int id)
        {
            var tag = await _context.ProductTags
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag == null) return NotFound();

            ViewBag.ProductName = tag.Product.Name;

            return View(new ProductTagFormViewModel
            {
                Id = tag.Id,
                ProductId = tag.ProductId,
                Name = tag.Name
            });
        }

        // POST: /Admin/Products/EditTag/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTag(
            int id,
            ProductTagFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var tag = await _context.ProductTags.FindAsync(id);

            if (tag == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                ViewBag.ProductName = product?.Name;

                return View(model);
            }

            tag.Name = model.Name.Trim();

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Update",
                "ProductTag",
                tag.Id.ToString(),
                $"Updated tag '{tag.Name}'");

            TempData["Success"] = "Tag updated.";

            return RedirectToAction(
                nameof(Tags),
                new { id = model.ProductId });
        }

        // POST: /Admin/Products/DeleteTag/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTag(
            int id,
            int productId)
        {
            var tag = await _context.ProductTags.FindAsync(id);

            if (tag == null) return NotFound();

            await _auditLogger.LogAsync(
                "Delete",
                "ProductTag",
                tag.Id.ToString(),
                tag.Name);

            _context.ProductTags.Remove(tag);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tag deleted.";

            return RedirectToAction(
                nameof(Tags),
                new { id = productId });
        }
    }
}