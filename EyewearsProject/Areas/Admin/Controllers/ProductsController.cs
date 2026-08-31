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

            return View(new ProductFormViewModel());
        }

        // POST: /Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(
                    model.CategoryId,
                    null,
                    model.BrandId);

                return View(model);
            }

            var productSku = model.Sku.Trim();

            // -----------------------------------------------------
            // Product SKU uniqueness
            // -----------------------------------------------------

            var skuExists = await _context.Products
                .AnyAsync(p => p.Sku == productSku);

            if (skuExists)
            {
                ModelState.AddModelError(
                    nameof(model.Sku),
                    "A product with this SKU already exists.");

                await PopulateDropdownsAsync(
                    model.CategoryId,
                    null,
                    model.BrandId);

                return View(model);
            }

            // -----------------------------------------------------
            // Discount validation
            // -----------------------------------------------------

            if (model.DiscountPrice.HasValue &&
                model.DiscountPrice.Value >= model.Price)
            {
                ModelState.AddModelError(
                    nameof(model.DiscountPrice),
                    "Discount price must be lower than the regular price.");

                await PopulateDropdownsAsync(
                    model.CategoryId,
                    null,
                    model.BrandId);

                return View(model);
            }

            // -----------------------------------------------------
            // Create product
            // -----------------------------------------------------

            var product = new Product
            {
                Name = model.Name.Trim(),

                Sku = productSku,

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

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(
                "Create",
                "Product",
                product.Id.ToString(),
                $"Created product {product.Name} (SKU: {product.Sku})");

            TempData["Success"] =
                "Product created. You can now add variants, images, specifications, attributes and tags.";

            return RedirectToAction(
                nameof(Edit),
                new { id = product.Id });
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

                            Sku = v.Sku,

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
            // -----------------------------------------------------
            // ID check
            // -----------------------------------------------------

            if (id != model.Id)
                return NotFound();

            // -----------------------------------------------------
            // Load complete product
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // Validation
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(
                    model.CategoryId,
                    model.SubCategoryId,
                    model.BrandId);

                return View(model);
            }

            // -----------------------------------------------------
            // Discount validation
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // Product SKU validation
            // -----------------------------------------------------

            var newProductSku =
                model.Sku.Trim();

            var duplicateProductSku =
                await _context.Products
                    .AnyAsync(p =>
                        p.Id != product.Id &&
                        p.Sku == newProductSku);

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

            // =====================================================
            // START TRANSACTION
            // =====================================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // =================================================
                // PRODUCT INFORMATION
                // =================================================

                product.Name =
                    model.Name.Trim();

                product.Sku =
                    newProductSku;

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

                // =================================================
                // PRICING
                // =================================================

                product.Price =
                    model.Price;

                product.DiscountPrice =
                    model.DiscountPrice;

                product.CostPrice =
                    model.CostPrice;

                product.Weight =
                    model.Weight;

                // =================================================
                // SETTINGS
                // =================================================

                product.IsActive =
                    model.IsActive;

                product.IsFeatured =
                    model.IsFeatured;

                // =================================================
                // VIRTUAL TRY-ON
                // =================================================

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

                // =================================================
                // IMAGES
                // =================================================

                await SaveImagesAsync(
                    product,
                    model);

                // =================================================
                // SPECIFICATIONS
                // =================================================

                await SaveSpecificationsAsync(
                    product,
                    model);

                // =================================================
                // ATTRIBUTES
                // =================================================

                await SaveAttributesAsync(
                    product,
                    model);

                // =================================================
                // TAGS
                // =================================================

                await SaveTagsAsync(
                    product,
                    model);

                // =================================================
                // FINAL SAVE
                // =================================================

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }

            // =====================================================
            // AUDIT
            // =====================================================

            await _auditLogger.LogAsync(
                "Update",
                "Product",
                product.Id.ToString(),
                $"Updated product {product.Name} including variants, inventory, images, specifications, attributes and tags.");

            TempData["Success"] =
                "Product updated successfully.";

            // Go back to Edit so manager immediately sees saved data.
            return RedirectToAction(
                nameof(Edit),
                new { id = product.Id });
        }

        // =========================================================
        // SAVE VARIANTS
        // =========================================================

        private async Task SaveVariantsAsync(
            Product product,
            ProductEditViewModel model)
        {
            var submittedVariants =
                model.Variants ??
                new List<ProductEditVariantViewModel>();

            // -----------------------------------------------------
            // Clean / validate submitted SKU list
            // -----------------------------------------------------

            var variantSkuList =
                submittedVariants
                    .Where(v =>
                        !string.IsNullOrWhiteSpace(v.Sku))
                    .Select(v => new
                    {
                        Id = v.Id,

                        Sku = v.Sku.Trim()
                    })
                    .ToList();

            // -----------------------------------------------------
            // Duplicate SKUs inside submitted form
            // -----------------------------------------------------

            var duplicateSubmittedSku =
                variantSkuList
                    .GroupBy(
                        v => v.Sku,
                        StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(g => g.Count() > 1);

            if (duplicateSubmittedSku != null)
            {
                ModelState.AddModelError(
                    "",
                    $"Variant SKU '{duplicateSubmittedSku.Key}' is used more than once.");

                throw new InvalidOperationException(
                    "Duplicate variant SKU.");
            }

            // -----------------------------------------------------
            // Check DB uniqueness
            // -----------------------------------------------------

            foreach (var variantSku in variantSkuList)
            {
                var exists =
                    await _context.ProductVariants
                        .AnyAsync(v =>
                            v.Id != variantSku.Id &&
                            v.Sku == variantSku.Sku);

                if (exists)
                {
                    ModelState.AddModelError(
                        "",
                        $"Variant SKU '{variantSku.Sku}' is already in use.");

                    throw new InvalidOperationException(
                        "Duplicate variant SKU.");
                }
            }

            // -----------------------------------------------------
            // IDs currently submitted
            // -----------------------------------------------------

            var submittedIds =
                submittedVariants
                    .Where(v => v.Id > 0)
                    .Select(v => v.Id)
                    .ToHashSet();

            // -----------------------------------------------------
            // Delete removed variants
            // -----------------------------------------------------

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

                    throw new InvalidOperationException(
                        "Variant has reserved inventory.");
                }

                await _auditLogger.LogAsync(
                    "Delete",
                    "ProductVariant",
                    variant.Id.ToString(),
                    $"Removed variant {variant.Color} from product #{product.Id}");

                _context.ProductVariants.Remove(variant);
            }

            // -----------------------------------------------------
            // Create / update variants
            // -----------------------------------------------------

            foreach (var variantModel in submittedVariants)
            {
                // =================================================
                // NEW VARIANT
                // =================================================

                if (variantModel.Id == 0)
                {
                    if (string.IsNullOrWhiteSpace(
                            variantModel.Color))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(
                            variantModel.Sku))
                    {
                        continue;
                    }

                    var newVariant =
                        new ProductVariant
                        {
                            ProductId =
                                product.Id,

                            Color =
                                variantModel.Color.Trim(),

                            Size =
                                string.IsNullOrWhiteSpace(
                                    variantModel.Size)
                                    ? null
                                    : variantModel.Size.Trim(),

                            Sku =
                                variantModel.Sku.Trim()
                        };

                    _context.ProductVariants.Add(
                        newVariant);

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
                        $"Added variant {newVariant.Color} to product #{product.Id}");

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

                variant.Sku =
                    variantModel.Sku?.Trim() ?? "";

                // -------------------------------------------------
                // Inventory
                // -------------------------------------------------

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

                if (variantModel.StockQuantity <
                    variant.Inventory.ReservedQuantity)
                {
                    ModelState.AddModelError(
                        "",
                        $"Stock for variant '{variant.Color}' cannot be lower than reserved quantity ({variant.Inventory.ReservedQuantity}).");

                    throw new InvalidOperationException(
                        "Stock below reserved quantity.");
                }

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

            // -----------------------------------------------------
            // IDs submitted
            // -----------------------------------------------------

            var submittedImageIds =
                orderedImages
                    .Where(i => i.Id > 0)
                    .Select(i => i.Id)
                    .ToHashSet();

            // -----------------------------------------------------
            // Delete removed images
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // Reset primary
            // -----------------------------------------------------

            foreach (var image in product.Images)
            {
                image.IsPrimary = false;
            }

            // -----------------------------------------------------
            // Update / create images
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // Delete removed
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // Create / update
            // -----------------------------------------------------

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

                // NEW
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

                // EXISTING
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

            // -----------------------------------------------------
            // Delete removed
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // Create / update
            // -----------------------------------------------------

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

                // NEW
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

                // EXISTING
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

            // -----------------------------------------------------
            // Remove old tags
            // -----------------------------------------------------

            var oldTags =
                product.ProductTags.ToList();

            foreach (var tag in oldTags)
            {
                _context.ProductTags.Remove(tag);
            }

            // -----------------------------------------------------
            // Add new tags
            // -----------------------------------------------------

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
        // DELETE PRODUCT
        // =========================================================

        // POST: /Admin/Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            await _auditLogger.LogAsync(
                "Delete",
                "Product",
                product.Id.ToString(),
                product.Name);

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product deleted successfully.";

            return RedirectToAction(
                nameof(Index));
        }

        // =========================================================
        // PREVIEW
        // =========================================================

        // GET: /Admin/Products/Preview/18
        //
        // IMPORTANT:
        // This loads the CUSTOMER-SIDE Details view directly.
        // Therefore manager sees the product as customer sees it.
        //
        // Unlike customer ProductsController/Details,
        // this does NOT require IsActive == true.
        // =========================================================

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

            // -----------------------------------------------------
            // Reviews for customer Details.cshtml
            // -----------------------------------------------------

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

            // Manager preview should not show review state
            // for the currently logged-in admin.
            ViewBag.UserAlreadyReviewed = false;

            ViewData["Title"] =
                $"Preview — {product.Name}";

            // -----------------------------------------------------
            // Re-use customer-side product page
            // -----------------------------------------------------

            return View(
                "~/Views/Products/Details.cshtml",
                product);
        }
    }
}