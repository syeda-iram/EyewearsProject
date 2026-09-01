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

        // =========================================================
        // DROPDOWNS
        // =========================================================

        private async Task PopulateDropdownsAsync(
            int? selectedCategoryId = null,
            int? selectedSubCategoryId = null,
            int? selectedBrandId = null)
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var parentCategories = categories
                .Where(c => c.ParentCategoryId == null)
                .ToList();

            var subCategories = categories
                .Where(c => c.ParentCategoryId != null)
                .ToList();

            var brands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                parentCategories,
                "Id",
                "Name",
                selectedCategoryId);

            ViewBag.SubCategories = new SelectList(
                subCategories,
                "Id",
                "Name",
                selectedSubCategoryId);

            ViewBag.Brands = new SelectList(
                brands,
                "Id",
                "Name",
                selectedBrandId);
        }

        // =========================================================
        // PRODUCTS LIST
        // =========================================================

        // GET: /Admin/Products
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewData["Title"] = "Products";

            return View(products);
        }

        // =========================================================
        // CREATE
        // =========================================================

        // GET: /Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();

            var model = new ProductFormViewModel
            {
                IsActive = true,
                IsFeatured = false,
                TryOnOverlayScale = 1,
                TryOnOverlayVerticalOffset = 0
            };

            return View(model);
        }

        // POST: /Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            // ---------------------------------------------------------
            // Normalize basic values
            // ---------------------------------------------------------

            model.Name = @model.Name?.Trim();
            model.Sku = model.Sku?.Trim();

            // ---------------------------------------------------------
            // Basic ModelState validation
            // ---------------------------------------------------------

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(
                    model.CategoryId,
                    model.SubCategoryId,
                    model.BrandId);

                return View(model);
            }

            // ---------------------------------------------------------
            // SKU required
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(model.Sku))
            {
                ModelState.AddModelError(
                    nameof(model.Sku),
                    "SKU is required.");

                await PopulateDropdownsAsync(
                    model.CategoryId,
                    model.SubCategoryId,
                    model.BrandId);

                return View(model);
            }

            // ---------------------------------------------------------
            // Product SKU uniqueness
            // ---------------------------------------------------------

            var skuExists = await _context.Products
                .AnyAsync(p => p.Sku == model.Sku);

            if (skuExists)
            {
                ModelState.AddModelError(
                    nameof(model.Sku),
                    "A product with this SKU already exists.");

                await PopulateDropdownsAsync(
                    model.CategoryId,
                    model.SubCategoryId,
                    model.BrandId);

                return View(model);
            }

            // ---------------------------------------------------------
            // Discount validation
            // ---------------------------------------------------------

            if (model.DiscountPrice.HasValue &&
                model.DiscountPrice.Value >= model.Price)
            {
                ModelState.AddModelError(
                    nameof(model.DiscountPrice),
                    "Discount price must be lower than the regular price.");

                await PopulateDropdownsAsync(
                    model.CategoryId,
                    model.SubCategoryId,
                    model.BrandId);

                return View(model);
            }

            // =========================================================
            // TRANSACTION
            // =========================================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // =====================================================
                // CREATE PRODUCT
                // =====================================================

                var product = new Product
                {
                    Name = model.Name ?? "",

                    Sku = model.Sku,

                    Description = model.Description,

                    CategoryId = model.CategoryId,

                    SubCategoryId = model.SubCategoryId,

                    BrandId = model.BrandId,

                    Gender = model.Gender,

                    Material = model.Material,

                    Shape = model.Shape,

                    Price = model.Price,

                    DiscountPrice = model.DiscountPrice,

                    CostPrice = model.CostPrice,

                    Weight = model.Weight,

                    IsFeatured = model.IsFeatured,

                    IsActive = model.IsActive,

                    TryOnOverlayImageUrl =
                        model.TryOnOverlayImageUrl,

                    TryOn3DModelUrl =
                        model.TryOn3DModelUrl,

                    TryOnOverlayScale =
                        model.TryOnOverlayScale,

                    TryOnOverlayVerticalOffset =
                        model.TryOnOverlayVerticalOffset,

                    CreatedAt = DateTime.UtcNow,

                    UpdatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);

                // Product ID required for child records.
                await _context.SaveChangesAsync();

                // =====================================================
                // VARIANTS + INVENTORY
                // =====================================================

                await CreateVariantsAsync(
                    product,
                    model);

                // =====================================================
                // IMAGES
                // =====================================================

                await CreateImagesAsync(
                    product,
                    model);

                // =====================================================
                // SPECIFICATIONS
                // =====================================================

                await CreateSpecificationsAsync(
                    product,
                    model);

                // =====================================================
                // ATTRIBUTES
                // =====================================================

                await CreateAttributesAsync(
                    product,
                    model);

                // =====================================================
                // TAGS
                // =====================================================

                await CreateTagsAsync(
                    product,
                    model);

                // =====================================================
                // FINAL SAVE
                // =====================================================

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // =====================================================
                // AUDIT
                // =====================================================

                await _auditLogger.LogAsync(
                    "Create",
                    "Product",
                    product.Id.ToString(),
                    $"Created product {product.Name} (SKU: {product.Sku}) including variants, inventory, images, specifications, attributes and tags.");

                TempData["Success"] =
                    "Product created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =========================================================
        // CREATE VARIANTS
        // =========================================================

        private async Task CreateVariantsAsync(
            Product product,
            ProductFormViewModel model)
        {
            // NOTE:
            // Product SKU is handled at product level.
            // Variants only contain Color + Size.
            // Inventory is maintained separately.

            if (model.Variants == null)
                return;

            foreach (var variantModel in model.Variants)
            {
                if (string.IsNullOrWhiteSpace(
                        variantModel.Color))
                {
                    continue;
                }

                var variant = new ProductVariant
                {
                    ProductId = product.Id,

                    Color = variantModel.Color.Trim(),

                    Size = string.IsNullOrWhiteSpace(
                        variantModel.Size)
                        ? null
                        : variantModel.Size.Trim()
                };

                _context.ProductVariants.Add(variant);

                // Need variant ID before creating inventory.
                await _context.SaveChangesAsync();

                await _inventoryService
                    .EnsureInventoryExistsAsync(
                        variant.Id);

                if (variantModel.StockQuantity > 0)
                {
                    await _inventoryService
                        .AdjustToQuantityAsync(
                            variant.Id,
                            variantModel.StockQuantity,
                            $"Initial stock added while creating variant ({variant.Color})");
                }

                await _auditLogger.LogAsync(
                    "Create",
                    "ProductVariant",
                    variant.Id.ToString(),
                    $"Added variant {variant.Color}" +
                    (string.IsNullOrWhiteSpace(variant.Size)
                        ? ""
                        : $" / {variant.Size}") +
                    $" to product #{product.Id}");
            }
        }

        // =========================================================
        // CREATE IMAGES
        // =========================================================

        private async Task CreateImagesAsync(
            Product product,
            ProductFormViewModel model)
        {
            if (model.ExistingImages == null)
                return;

            var images = model.ExistingImages
                .Where(i =>
                    !string.IsNullOrWhiteSpace(i.ImageUrl))
                .OrderBy(i => i.SortOrder)
                .ToList();

            for (var index = 0;
                 index < images.Count;
                 index++)
            {
                var imageModel = images[index];

                var image = new ProductImage
                {
                    ProductId = product.Id,

                    ImageUrl = imageModel.ImageUrl.Trim(),

                    IsPrimary = index == 0,

                    ProductVariantId =
                        imageModel.ProductVariantId
                };

                _context.ProductImages.Add(image);

                await _auditLogger.LogAsync(
                    "Create",
                    "ProductImage",
                    "New",
                    $"Added image {image.ImageUrl} to product #{product.Id}");
            }
        }

        // =========================================================
        // CREATE SPECIFICATIONS
        // =========================================================

        private async Task CreateSpecificationsAsync(
            Product product,
            ProductFormViewModel model)
        {
            if (model.Specifications == null)
                return;

            foreach (var specificationModel
                     in model.Specifications)
            {
                if (string.IsNullOrWhiteSpace(
                        specificationModel.Name) ||
                    string.IsNullOrWhiteSpace(
                        specificationModel.Value))
                {
                    continue;
                }

                var specification =
                    new ProductSpecification
                    {
                        ProductId = product.Id,

                        Name =
                            specificationModel.Name.Trim(),

                        Value =
                            specificationModel.Value.Trim(),

                        SortOrder =
                            specificationModel.SortOrder
                    };

                _context.ProductSpecifications
                    .Add(specification);
            }

            await Task.CompletedTask;
        }

        // =========================================================
        // CREATE ATTRIBUTES
        // =========================================================

        private async Task CreateAttributesAsync(
            Product product,
            ProductFormViewModel model)
        {
            if (model.Attributes == null)
                return;

            foreach (var attributeModel
                     in model.Attributes)
            {
                if (string.IsNullOrWhiteSpace(
                        attributeModel.Name) ||
                    string.IsNullOrWhiteSpace(
                        attributeModel.Value))
                {
                    continue;
                }

                var attribute =
                    new ProductAttribute
                    {
                        ProductId = product.Id,

                        Name =
                            attributeModel.Name.Trim(),

                        Value =
                            attributeModel.Value.Trim(),

                        SortOrder =
                            attributeModel.SortOrder
                    };

                _context.ProductAttributes
                    .Add(attribute);
            }

            await Task.CompletedTask;
        }

        // =========================================================
        // CREATE TAGS
        // =========================================================

        private async Task CreateTagsAsync(
            Product product,
            ProductFormViewModel model)
        {
            if (model.Tags == null)
                return;

            var cleanTags = model.Tags
                .Where(t =>
                    !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var tagName in cleanTags)
            {
                _context.ProductTags.Add(
                    new ProductTag
                    {
                        ProductId = product.Id,
                        Name = tagName
                    });
            }

            await Task.CompletedTask;
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        // GET: /Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products

                .Include(p => p.Category)

                .Include(p => p.SubCategory)

                .Include(p => p.Brand)

                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)

                .Include(p => p.Images)

                .Include(p => p.Specifications)

                .Include(p => p.Attributes)

                .Include(p => p.ProductTags)

                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            var model = new ProductEditViewModel
            {
                Id = product.Id,

                // =================================================
                // BASIC INFORMATION
                // =================================================

                Name = product.Name,

                Sku = product.Sku,

                Description = product.Description,

                // =================================================
                // CATEGORY
                // =================================================

                CategoryId = product.CategoryId,

                SubCategoryId = product.SubCategoryId,

                BrandId = product.BrandId,

                // =================================================
                // PRODUCT DETAILS
                // =================================================

                Gender = product.Gender,

                Material = product.Material,

                Shape = product.Shape,

                // =================================================
                // PRICING
                // =================================================

                Price = product.Price,

                DiscountPrice = product.DiscountPrice,

                CostPrice = product.CostPrice,

                Weight = product.Weight,

                // =================================================
                // SETTINGS
                // =================================================

                IsActive = product.IsActive,

                IsFeatured = product.IsFeatured,

                // =================================================
                // TRY ON
                // =================================================

                TryOnOverlayImageUrl =
                    product.TryOnOverlayImageUrl,

                TryOn3DModelUrl =
                    product.TryOn3DModelUrl,

                TryOnOverlayScale =
                    product.TryOnOverlayScale,

                TryOnOverlayVerticalOffset =
                    product.TryOnOverlayVerticalOffset,

                // =================================================
                // VARIANTS
                // =================================================

                Variants = product.Variants
                    .OrderBy(v => v.Id)
                    .Select(v =>
                        new ProductEditVariantViewModel
                        {
                            Id = v.Id,

                            Color = v.Color,

                            Size = v.Size,

                            StockQuantity =
                                v.Inventory?.QuantityOnHand ?? 0,

                            ReservedQuantity =
                                v.Inventory?.ReservedQuantity ?? 0
                        })
                    .ToList(),

                // =================================================
                // IMAGES
                // =================================================

                ExistingImages = product.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.Id)
                    .Select((image, index) =>
                        new ProductEditImageViewModel
                        {
                            Id = image.Id,

                            ImageUrl = image.ImageUrl,

                            IsPrimary = image.IsPrimary,

                            ProductVariantId =
                                image.ProductVariantId,

                            SortOrder = index
                        })
                    .ToList(),

                // =================================================
                // SPECIFICATIONS
                // =================================================

                Specifications = product.Specifications
                    .OrderBy(s => s.SortOrder)
                    .ThenBy(s => s.Id)
                    .Select(s =>
                        new ProductEditSpecificationViewModel
                        {
                            Id = s.Id,

                            Name = s.Name,

                            Value = s.Value,

                            SortOrder = s.SortOrder
                        })
                    .ToList(),

                // =================================================
                // ATTRIBUTES
                // =================================================

                Attributes = product.Attributes
                    .OrderBy(a => a.SortOrder)
                    .ThenBy(a => a.Id)
                    .Select(a =>
                        new ProductAttributeFormViewModel
                        {
                            Id = a.Id,

                            ProductId = a.ProductId,

                            Name = a.Name,

                            Value = a.Value,

                            SortOrder = a.SortOrder
                        })
                    .ToList(),

                // =================================================
                // TAGS
                // =================================================

                Tags = product.ProductTags
                    .OrderBy(t => t.Name)
                    .Select(t => t.Name)
                    .ToList()
            };

            await PopulateDropdownsAsync(
                product.CategoryId,
                product.SubCategoryId,
                product.BrandId);

            ViewData["Title"] =
                $"Edit Product — {product.Name}";

            return View(model);
        }

        // =========================================================
        // EDIT - POST
        // SAVE EVERYTHING
        // =========================================================

        // POST: /Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ProductEditViewModel model)
        {
            // ---------------------------------------------------------
            // ID validation
            // ---------------------------------------------------------

            if (id != model.Id)
                return NotFound();

            // ---------------------------------------------------------
            // Normalize
            // ---------------------------------------------------------

            model.Name = model.Name?.Trim();
            model.Sku = model.Sku?.Trim();

            // ---------------------------------------------------------
            // Load complete product
            // ---------------------------------------------------------

            var product = await _context.Products

                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)

                .Include(p => p.Images)

                .Include(p => p.Specifications)

                .Include(p => p.Attributes)

                .Include(p => p.ProductTags)

                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            // ---------------------------------------------------------
            // Model validation
            // ---------------------------------------------------------

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(
                    model.CategoryId,
                    model.SubCategoryId,
                    model.BrandId);

                return View(model);
            }

            // ---------------------------------------------------------
            // SKU required
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(model.Sku))
            {
                ModelState.AddModelError(
                    nameof(model.Sku),
                    "SKU is required.");

                await PopulateDropdownsAsync(
                    model.CategoryId,
                    model.SubCategoryId,
                    model.BrandId);

                return View(model);
            }

            // ---------------------------------------------------------
            // Discount validation
            // ---------------------------------------------------------

            if (model.DiscountPrice.HasValue &&
                model.DiscountPrice.Value >= model.Price)
            {
                ModelState.AddModelError(
                    nameof(model.DiscountPrice),
                    "Discount price must be lower than the regular price.");

                await PopulateDropdownsAsync(
                    model.CategoryId,
                    model.SubCategoryId,
                    model.BrandId);

                return View(model);
            }

            // ---------------------------------------------------------
            // Product SKU uniqueness
            // ---------------------------------------------------------

            var duplicateProductSku =
                await _context.Products
                    .AnyAsync(p =>
                        p.Id != product.Id &&
                        p.Sku == model.Sku);

            if (duplicateProductSku)
            {
                ModelState.AddModelError(
                    nameof(model.Sku),
                    "Another product already uses this SKU.");

                await PopulateDropdownsAsync(
                    model.CategoryId,
                    model.SubCategoryId,
                    model.BrandId);

                return View(model);
            }

            // =========================================================
            // TRANSACTION
            // =========================================================

            // =====================================================
            // TRANSACTION — wrapped in EF's retry-aware execution
            // strategy, required once EnableRetryOnFailure is on
            // =====================================================

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

            try
            {
                // =====================================================
                // PRODUCT INFORMATION
                // =====================================================

                    product.Name =
                        model.Name.Trim();

                    product.Sku =
                        model.Sku;

                    product.Description =
                        model.Description;

                    product.CategoryId =
                        model.CategoryId;

                    product.SubCategoryId =
                        model.SubCategoryId;

                    product.BrandId =
                        model.BrandId;

                    product.Gender =
                        model.Gender;

                    product.Material =
                        model.Material;

                    product.Shape =
                        model.Shape;

                // =====================================================
                // PRICING
                // =====================================================

                    product.Price =
                        model.Price;

                    product.DiscountPrice =
                        model.DiscountPrice;

                    product.CostPrice =
                        model.CostPrice;

                    product.Weight =
                        model.Weight;

                // =====================================================
                // SETTINGS
                // =====================================================

                    product.IsActive =
                        model.IsActive;

                    product.IsFeatured =
                        model.IsFeatured;

                // =====================================================
                // VIRTUAL TRY-ON
                // =====================================================

                    product.TryOnOverlayImageUrl =
                        model.TryOnOverlayImageUrl;

                    product.TryOn3DModelUrl =
                        model.TryOn3DModelUrl;

                    product.TryOnOverlayScale =
                        model.TryOnOverlayScale;

                    product.TryOnOverlayVerticalOffset =
                        model.TryOnOverlayVerticalOffset;

                    product.UpdatedAt =
                        DateTime.UtcNow;

                    // =================================================
                    // VARIANTS
                    // =================================================

                    await SaveVariantsAsync(
                        product,
                        model);

                // =====================================================
                // IMAGES
                // =====================================================

                    await SaveImagesAsync(
                        product,
                        model);

                // =====================================================
                // SPECIFICATIONS
                // =====================================================

                    await SaveSpecificationsAsync(
                        product,
                        model);

                // =====================================================
                // ATTRIBUTES
                // =====================================================

                    await SaveAttributesAsync(
                        product,
                        model);

                // =====================================================
                // TAGS
                // =====================================================

                    await SaveTagsAsync(
                        product,
                        model);

                // =====================================================
                // FINAL SAVE
                // =====================================================

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();

                    throw;
                }
            });
            // =====================================================
            // AUDIT
            // =========================================================

            await _auditLogger.LogAsync(
                "Update",
                "Product",
                product.Id.ToString(),
                $"Updated product {product.Name} including variants, inventory, images, specifications, attributes and tags.");

            TempData["Success"] =
                "Product updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // SAVE VARIANTS
        // =========================================================

        private async Task<bool> SaveVariantsAsync(
            Product product,
            ProductEditViewModel model)
        {
            var submittedVariants =
                model.Variants ??
                new List<ProductEditVariantViewModel>();

            // =====================================================
            // VALIDATE VARIANTS
            // =====================================================

            foreach (var variantModel in submittedVariants)
            {
                if (variantModel.Id == 0 &&
                    string.IsNullOrWhiteSpace(
                        variantModel.Color))
                {
                    continue;
                }

                if (variantModel.Id > 0 &&
                    string.IsNullOrWhiteSpace(
                        variantModel.Color))
                {
                    ModelState.AddModelError(
                        "",
                        "Variant color is required.");

                    return false;
                }
            }

            // =====================================================
            // PREVENT DUPLICATE COLOR + SIZE COMBINATIONS
            // =====================================================

            var duplicateVariant =
                submittedVariants
                    .Where(v =>
                        !string.IsNullOrWhiteSpace(v.Color))
                    .GroupBy(v => new
                    {
                        Color = v.Color!.Trim(),
                        Size = string.IsNullOrWhiteSpace(v.Size)
                            ? null
                            : v.Size.Trim()
                    })
                    .FirstOrDefault(g => g.Count() > 1);

            if (duplicateVariant != null)
            {
                var display =
                    duplicateVariant.Key.Color +
                    (duplicateVariant.Key.Size == null
                        ? ""
                        : $" / {duplicateVariant.Key.Size}");

                ModelState.AddModelError(
                    "",
                    $"Variant '{display}' already exists.");

                return false;
            }

            // =====================================================
            // SUBMITTED EXISTING IDs
            // =====================================================

            var submittedIds =
                submittedVariants
                    .Where(v => v.Id > 0)
                    .Select(v => v.Id)
                    .ToHashSet();

            // =====================================================
            // DELETE REMOVED VARIANTS
            // =====================================================

            var variantsToDelete =
                product.Variants
                    .Where(v =>
                        !submittedIds.Contains(v.Id))
                    .ToList();

            foreach (var variant in variantsToDelete)
            {
                if (variant.Inventory != null &&
                    variant.Inventory.ReservedQuantity > 0)
                {
                    ModelState.AddModelError(
                        "",
                        $"Variant '{variant.Color}' cannot be deleted because it has reserved stock.");

                    return false;
                }

                await _auditLogger.LogAsync(
                    "Delete",
                    "ProductVariant",
                    variant.Id.ToString(),
                    $"Removed variant {variant.Color} from product #{product.Id}");

                _context.ProductVariants.Remove(variant);
            }

            // =====================================================
            // CREATE / UPDATE VARIANTS
            // =====================================================

            foreach (var variantModel in submittedVariants)
            {
                // -------------------------------------------------
                // Skip completely empty new rows
                // -------------------------------------------------

                if (variantModel.Id == 0 &&
                    string.IsNullOrWhiteSpace(
                        variantModel.Color))
                {
                    continue;
                }

                // =================================================
                // NEW VARIANT
                // =================================================

                if (variantModel.Id == 0)
                {
                    var newVariant =
                        new ProductVariant
                        {
                            ProductId =
                                product.Id,

                            Color =
                                variantModel.Color!.Trim(),

                            Size =
                                string.IsNullOrWhiteSpace(
                                    variantModel.Size)
                                    ? null
                                    : variantModel.Size.Trim()
                        };

                    _context.ProductVariants.Add(
                        newVariant);

                    // Need ID for inventory.
                    await _context.SaveChangesAsync();

                    await _inventoryService
                        .EnsureInventoryExistsAsync(
                            newVariant.Id);

                    if (variantModel.StockQuantity > 0)
                    {
                        await _inventoryService
                            .AdjustToQuantityAsync(
                                newVariant.Id,
                                variantModel.StockQuantity,
                                $"Initial stock added while creating variant ({newVariant.Color})");
                    }

                    await _auditLogger.LogAsync(
                        "Create",
                        "ProductVariant",
                        newVariant.Id.ToString(),
                        $"Added variant {newVariant.Color}" +
                        (string.IsNullOrWhiteSpace(
                            newVariant.Size)
                            ? ""
                            : $" / {newVariant.Size}") +
                        $" to product #{product.Id}");

                    continue;
                }

                // =================================================
                // EXISTING VARIANT
                // =================================================

                var variant =
                    product.Variants
                        .FirstOrDefault(v =>
                            v.Id == variantModel.Id);

                if (variant == null)
                    continue;

                variant.Color =
                    variantModel.Color?.Trim() ?? "";

                variant.Size =
                    string.IsNullOrWhiteSpace(
                        variantModel.Size)
                        ? null
                        : variantModel.Size.Trim();

                // =================================================
                // INVENTORY
                // =================================================

                if (variant.Inventory == null)
                {
                    await _inventoryService
                        .EnsureInventoryExistsAsync(
                            variant.Id);

                    variant.Inventory =
                        await _context.Inventories
                            .FirstAsync(i =>
                                i.ProductVariantId ==
                                variant.Id);
                }

                // -------------------------------------------------
                // Cannot reduce stock below reserved quantity
                // -------------------------------------------------

                if (variantModel.StockQuantity <
                    variant.Inventory.ReservedQuantity)
                {
                    ModelState.AddModelError(
                        "",
                        $"Stock for variant '{variant.Color}' cannot be lower than reserved quantity ({variant.Inventory.ReservedQuantity}).");

                    return false;
                }

                // -------------------------------------------------
                // Adjust inventory
                // -------------------------------------------------

                if (variant.Inventory.QuantityOnHand !=
                    variantModel.StockQuantity)
                {
                    await _inventoryService
                        .AdjustToQuantityAsync(
                            variant.Id,
                            variantModel.StockQuantity,
                            $"Stock updated from product edit form ({variant.Color})");
                }
            }
            return true;
        }

        // =========================================================
        // SAVE IMAGES
        // =========================================================

        private async Task SaveImagesAsync(
            Product product,
            ProductEditViewModel model)
        {
            if (model.ExistingImages == null)
                return;

            var orderedImages =
                model.ExistingImages
                    .Where(i =>
                        !string.IsNullOrWhiteSpace(
                            i.ImageUrl))
                    .OrderBy(i => i.SortOrder)
                    .ToList();

            // =====================================================
            // SUBMITTED IDs
            // =====================================================

            var submittedImageIds =
                orderedImages
                    .Where(i => i.Id > 0)
                    .Select(i => i.Id)
                    .ToHashSet();

            // =====================================================
            // DELETE REMOVED IMAGES
            // =====================================================

            var imagesToDelete =
                product.Images
                    .Where(i =>
                        !submittedImageIds.Contains(i.Id))
                    .ToList();

            foreach (var image in imagesToDelete)
            {
                await _auditLogger.LogAsync(
                    "Delete",
                    "ProductImage",
                    image.Id.ToString(),
                    image.ImageUrl);

                _context.ProductImages.Remove(image);
            }

            // =====================================================
            // RESET PRIMARY
            // =====================================================

            foreach (var image in product.Images)
            {
                image.IsPrimary = false;
            }

            // =====================================================
            // UPDATE / CREATE
            // =====================================================

            for (var index = 0;
                 index < orderedImages.Count;
                 index++)
            {
                var imageModel =
                    orderedImages[index];

                var isPrimary =
                    index == 0;

                // -------------------------------------------------
                // NEW IMAGE
                // -------------------------------------------------

                if (imageModel.Id == 0)
                {
                    var newImage =
                        new ProductImage
                        {
                            ProductId =
                                product.Id,

                            ImageUrl =
                                imageModel.ImageUrl.Trim(),

                            IsPrimary =
                                isPrimary,

                            ProductVariantId =
                                imageModel.ProductVariantId
                        };

                    _context.ProductImages.Add(
                        newImage);

                    await _auditLogger.LogAsync(
                        "Create",
                        "ProductImage",
                        "New",
                        $"Added image {newImage.ImageUrl} to product #{product.Id}");

                    continue;
                }

                // -------------------------------------------------
                // EXISTING IMAGE
                // -------------------------------------------------

                var existingImage =
                    product.Images
                        .FirstOrDefault(i =>
                            i.Id == imageModel.Id);

                if (existingImage == null)
                    continue;

                existingImage.ImageUrl =
                    imageModel.ImageUrl.Trim();

                existingImage.IsPrimary =
                    isPrimary;

                existingImage.ProductVariantId =
                    imageModel.ProductVariantId;
            }
        }

        // =========================================================
        // SAVE SPECIFICATIONS
        // =========================================================

        private async Task SaveSpecificationsAsync(
            Product product,
            ProductEditViewModel model)
        {
            var submittedSpecifications =
                model.Specifications ??
                new List<ProductEditSpecificationViewModel>();

            var submittedIds =
                submittedSpecifications
                    .Where(s => s.Id > 0)
                    .Select(s => s.Id)
                    .ToHashSet();

            // =====================================================
            // DELETE REMOVED
            // =====================================================

            var specificationsToDelete =
                product.Specifications
                    .Where(s =>
                        !submittedIds.Contains(s.Id))
                    .ToList();

            foreach (var specification
                     in specificationsToDelete)
            {
                await _auditLogger.LogAsync(
                    "Delete",
                    "ProductSpecification",
                    specification.Id.ToString(),
                    specification.Name);

                _context.ProductSpecifications
                    .Remove(specification);
            }

            // =====================================================
            // CREATE / UPDATE
            // =====================================================

            foreach (var specificationModel
                     in submittedSpecifications)
            {
                if (string.IsNullOrWhiteSpace(
                        specificationModel.Name) ||
                    string.IsNullOrWhiteSpace(
                        specificationModel.Value))
                {
                    continue;
                }

                // -------------------------------------------------
                // NEW
                // -------------------------------------------------

                if (specificationModel.Id == 0)
                {
                    var specification =
                        new ProductSpecification
                        {
                            ProductId =
                                product.Id,

                            Name =
                                specificationModel.Name.Trim(),

                            Value =
                                specificationModel.Value.Trim(),

                            SortOrder =
                                specificationModel.SortOrder
                        };

                    _context.ProductSpecifications
                        .Add(specification);

                    continue;
                }

                // -------------------------------------------------
                // EXISTING
                // -------------------------------------------------

                var existingSpecification =
                    product.Specifications
                        .FirstOrDefault(s =>
                            s.Id ==
                            specificationModel.Id);

                if (existingSpecification == null)
                    continue;

                existingSpecification.Name =
                    specificationModel.Name.Trim();

                existingSpecification.Value =
                    specificationModel.Value.Trim();

                existingSpecification.SortOrder =
                    specificationModel.SortOrder;
            }
        }

        // =========================================================
        // SAVE ATTRIBUTES
        // =========================================================

        private async Task SaveAttributesAsync(
            Product product,
            ProductEditViewModel model)
        {
            var submittedAttributes =
                model.Attributes ??
                new List<ProductAttributeFormViewModel>();

            var submittedIds =
                submittedAttributes
                    .Where(a => a.Id > 0)
                    .Select(a => a.Id)
                    .ToHashSet();

            // =====================================================
            // DELETE REMOVED
            // =====================================================

            var attributesToDelete =
                product.Attributes
                    .Where(a =>
                        !submittedIds.Contains(a.Id))
                    .ToList();

            foreach (var attribute
                     in attributesToDelete)
            {
                await _auditLogger.LogAsync(
                    "Delete",
                    "ProductAttribute",
                    attribute.Id.ToString(),
                    attribute.Name);

                _context.ProductAttributes
                    .Remove(attribute);
            }

            // =====================================================
            // CREATE / UPDATE
            // =====================================================

            foreach (var attributeModel
                     in submittedAttributes)
            {
                if (string.IsNullOrWhiteSpace(
                        attributeModel.Name) ||
                    string.IsNullOrWhiteSpace(
                        attributeModel.Value))
                {
                    continue;
                }

                // -------------------------------------------------
                // NEW
                // -------------------------------------------------

                if (attributeModel.Id == 0)
                {
                    var attribute =
                        new ProductAttribute
                        {
                            ProductId =
                                product.Id,

                            Name =
                                attributeModel.Name.Trim(),

                            Value =
                                attributeModel.Value.Trim(),

                            SortOrder =
                                attributeModel.SortOrder
                        };

                    _context.ProductAttributes
                        .Add(attribute);

                    continue;
                }

                // -------------------------------------------------
                // EXISTING
                // -------------------------------------------------

                var existingAttribute =
                    product.Attributes
                        .FirstOrDefault(a =>
                            a.Id ==
                            attributeModel.Id);

                if (existingAttribute == null)
                    continue;

                existingAttribute.Name =
                    attributeModel.Name.Trim();

                existingAttribute.Value =
                    attributeModel.Value.Trim();

                existingAttribute.SortOrder =
                    attributeModel.SortOrder;
            }
        }

        // =========================================================
        // SAVE TAGS
        // =========================================================

        private async Task SaveTagsAsync(
            Product product,
            ProductEditViewModel model)
        {
            var submittedTags =
                model.Tags ??
                new List<string>();

            var cleanTags =
                submittedTags
                    .Where(t =>
                        !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            // =====================================================
            // REMOVE OLD TAGS
            // =====================================================

            var oldTags =
                product.ProductTags.ToList();

            foreach (var tag in oldTags)
            {
                _context.ProductTags.Remove(tag);
            }

            // =====================================================
            // ADD NEW TAGS
            // =====================================================

            foreach (var tagName in cleanTags)
            {
                _context.ProductTags.Add(
                    new ProductTag
                    {
                        ProductId =
                            product.Id,

                        Name =
                            tagName
                    });
            }

            await Task.CompletedTask;
        }

        // =========================================================
        // TOGGLE ACTIVE
        // =========================================================

        // POST: /Admin/Products/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var product =
                await _context.Products
                    .FindAsync(id);

            if (product == null)
                return NotFound();

            product.IsActive =
                !product.IsActive;

            product.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                product.IsActive
                    ? "Activate"
                    : "Deactivate",
                "Product",
                product.Id.ToString(),
                product.Name);

            TempData["Success"] =
                product.IsActive
                    ? "Product activated."
                    : "Product deactivated.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // PREVIEW
        // =========================================================

        // GET: /Admin/Products/Preview/18
        public async Task<IActionResult> Preview(int id)
        {
            var product = await _context.Products

                .AsNoTracking()

                .Include(p => p.Brand)

                .Include(p => p.Category)

                .Include(p => p.SubCategory)

                .Include(p => p.Images)
                    .ThenInclude(i => i.ProductVariant)

                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)

                .Include(p => p.Specifications)

                .Include(p => p.Attributes)

                .Include(p => p.ProductTags)

                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            // =====================================================
            // APPROVED REVIEWS
            // =====================================================

            var approvedReviews =
                await _context.Reviews
                    .Include(r => r.User)
                    .Where(r =>
                        r.ProductId == id &&
                        r.IsApproved)
                    .OrderByDescending(
                        r => r.CreatedAt)
                    .ToListAsync();

            ViewBag.ApprovedReviews =
                approvedReviews;

            ViewBag.AverageRating =
                approvedReviews.Any()
                    ? approvedReviews.Average(
                        r => r.Rating)
                    : (double?)null;

            // Admin preview should not show
            // customer's own review state.
            ViewBag.UserAlreadyReviewed =
                false;

            ViewData["Title"] =
                $"Preview — {product.Name}";

            return View(
                "~/Views/Products/Details.cshtml",
                product);
        }
    }
}