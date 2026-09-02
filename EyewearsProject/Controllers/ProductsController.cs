using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EyewearsProject.Models;

namespace EyewearsProject.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Categories that exist in the DB (so they show in nav/menus) but have no
        // real products behind them yet — show a friendly placeholder instead of
        // an empty grid. Add more names here as needed.
        private static readonly string[] ComingSoonCategories = { "Accessories", "Contact Lenses" };

        // =====================================================
        // GET: /Products
        // =====================================================

        public async Task<IActionResult> Index(
            string? search,
            string? category,
            int? subCategoryId,
            int? brandId,
            string? color,
            string? gender,
            string? material,
            string? shape,
            string? tag,
            decimal? minPrice,
            decimal? maxPrice,
            string? spec)
        {
            if (!string.IsNullOrWhiteSpace(category) &&
                ComingSoonCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                ViewBag.CategoryName = category;
                return View("ComingSoon");
            }

            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Include(p => p.Specifications)
                .Include(p => p.Attributes)
                .Include(p => p.ProductTags)
                .Where(p => p.IsActive)
                .AsQueryable();

            // =================================================
            // SEARCH
            // =================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Sku.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)) ||
                    (p.Brand != null && p.Brand.Name.Contains(search)) ||
                    (p.Category != null && p.Category.Name.Contains(search)) ||
                    (p.SubCategory != null && p.SubCategory.Name.Contains(search)) ||
                    (p.Gender != null && p.Gender.Contains(search)) ||
                    (p.Material != null && p.Material.Contains(search)) ||
                    (p.Shape != null && p.Shape.Contains(search)) ||
                    p.Variants.Any(v => v.Color.Contains(search)) ||
                    p.ProductTags.Any(t => t.Name.Contains(search)) ||
                    p.Specifications.Any(s =>
                        s.Name.Contains(search) ||
                        s.Value.Contains(search))
                );
            }

            // =================================================
            // CATEGORY
            // =================================================

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p =>
                    p.Category.Name == category);
            }

            // =================================================
            // SUBCATEGORY
            // =================================================

            if (subCategoryId.HasValue)
            {
                query = query.Where(p =>
                    p.SubCategoryId == subCategoryId.Value);
            }

            // =================================================
            // BRAND
            // =================================================

            if (brandId.HasValue)
            {
                query = query.Where(p =>
                    p.BrandId == brandId.Value);
            }

            // =================================================
            // COLOR
            //
            // Color exists only at VARIANT level.
            // =================================================

            if (!string.IsNullOrWhiteSpace(color))
            {
                query = query.Where(p =>
                    p.Variants.Any(v =>
                        v.Color == color));
            }

            // =================================================
            // GENDER
            // =================================================

            if (!string.IsNullOrWhiteSpace(gender))
            {
                query = query.Where(p =>
                    p.Gender == gender);
            }

            // =================================================
            // MATERIAL
            // =================================================

            if (!string.IsNullOrWhiteSpace(material))
            {
                query = query.Where(p =>
                    p.Material == material);
            }

            // =================================================
            // SHAPE
            // =================================================

            if (!string.IsNullOrWhiteSpace(shape))
            {
                query = query.Where(p =>
                    p.Shape == shape);
            }

            // =================================================
            // PRODUCT TAG
            // =================================================

            if (!string.IsNullOrWhiteSpace(tag))
            {
                query = query.Where(p =>
                    p.ProductTags.Any(t =>
                        t.Name == tag));
            }

            // =================================================
            // PRICE
            // =================================================

            if (minPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.DiscountPrice ?? p.Price) >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
            }

            // =================================================
            // DYNAMIC SPECIFICATION
            //
            // Example:
            // Material:Acetate
            // Lens Type:UV Protection
            // =================================================

            if (!string.IsNullOrWhiteSpace(spec))
            {
                var parts = spec.Split(':', 2);

                if (parts.Length == 2)
                {
                    var specName = parts[0].Trim();
                    var specValue = parts[1].Trim();

                    query = query.Where(p =>
                        p.Specifications.Any(s =>
                            s.Name == specName &&
                            s.Value == specValue));
                }
            }

            // =================================================
            // ORDERING
            // =================================================

            var products = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            // =================================================
            // FILTER / FACET DATA
            // =================================================

            // -------------------------------------------------
            // Categories
            // -------------------------------------------------

            ViewBag.AllCategories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            // -------------------------------------------------
            // Subcategories — only children of the currently selected
            // top-level category, not every subcategory in the system.
            // -------------------------------------------------

            int? selectedCategoryId = null;
            if (!string.IsNullOrWhiteSpace(category))
            {
                selectedCategoryId = (await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == category))?.Id;
            }

            ViewBag.AllSubCategories = await _context.Categories
                .Where(c => c.ParentCategoryId != null &&
                            (selectedCategoryId == null || c.ParentCategoryId == selectedCategoryId))
                .OrderBy(c => c.Name)
                .ToListAsync();

            // -------------------------------------------------
            // Brands
            // -------------------------------------------------

            ViewBag.AllBrands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();

            // -------------------------------------------------
            // Colors
            //
            // Colors come ONLY from ProductVariant.
            // -------------------------------------------------

            ViewBag.AllColors = await _context.ProductVariants
                .Where(v =>
                    v.Product.IsActive &&
                    v.Color != null &&
                    v.Color != "")
                .Select(v => v.Color)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // -------------------------------------------------
            // Genders
            // -------------------------------------------------

            ViewBag.AllGenders = await _context.Products
                .Where(p =>
                    p.IsActive &&
                    p.Gender != null &&
                    p.Gender != "")
                .Select(p => p.Gender!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // -------------------------------------------------
            // Materials
            // -------------------------------------------------

            ViewBag.AllMaterials = await _context.Products
                .Where(p =>
                    p.IsActive &&
                    p.Material != null &&
                    p.Material != "")
                .Select(p => p.Material!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // -------------------------------------------------
            // Shapes
            // -------------------------------------------------

            ViewBag.AllShapes = await _context.Products
                .Where(p =>
                    p.IsActive &&
                    p.Shape != null &&
                    p.Shape != "")
                .Select(p => p.Shape!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // -------------------------------------------------
            // Product Tags
            // -------------------------------------------------

            ViewBag.AllTags = await _context.ProductTags
                .Where(t =>
                    t.Product.IsActive &&
                    t.Name != null &&
                    t.Name != "")
                .Select(t => t.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // -------------------------------------------------
            // Dynamic Specifications
            // -------------------------------------------------

            ViewBag.AllSpecs = await _context.ProductSpecifications
                .Where(s =>
                    s.Product.IsActive &&
                    s.Name != null &&
                    s.Name != "" &&
                    s.Value != null &&
                    s.Value != "")
                .Select(s => new
                {
                    s.Name,
                    s.Value
                })
                .Distinct()
                .OrderBy(s => s.Name)
                .ThenBy(s => s.Value)
                .ToListAsync();

            // =================================================
            // SELECTED FILTERS
            // =================================================

            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedSubCategoryId = subCategoryId;
            ViewBag.SelectedBrandId = brandId;
            ViewBag.SelectedColor = color;
            ViewBag.SelectedGender = gender;
            ViewBag.SelectedMaterial = material;
            ViewBag.SelectedShape = shape;
            ViewBag.SelectedTag = tag;
            ViewBag.SelectedMinPrice = minPrice;
            ViewBag.SelectedMaxPrice = maxPrice;
            ViewBag.SelectedSpec = spec;

            return View(products);
        }

        // =====================================================
        // GET: /Products/Details/5
        // =====================================================

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
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
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.IsActive);

            if (product == null)
                return NotFound();

            // =================================================
            // APPROVED REVIEWS
            // =================================================

            var approvedReviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r =>
                    r.ProductId == id &&
                    r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.ApprovedReviews = approvedReviews;

            // =================================================
            // AVERAGE RATING
            // =================================================

            ViewBag.AverageRating =
                approvedReviews.Any()
                    ? approvedReviews.Average(r => r.Rating)
                    : (double?)null;

            // =================================================
            // CURRENT USER REVIEW STATUS
            // =================================================

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);

                ViewBag.UserAlreadyReviewed =
                    await _context.Reviews.AnyAsync(r =>
                        r.ProductId == id &&
                        r.UserId == userId);
            }

            return View(product);
        }

        // =====================================================
        // POST: /Products/SubmitReview
        // =====================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(
            int productId,
            int rating,
            string? comment)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // Make sure product exists and is active.
            var productExists = await _context.Products
                .AnyAsync(p =>
                    p.Id == productId &&
                    p.IsActive);

            if (!productExists)
                return NotFound();

            // One review per user per product.
            bool alreadyReviewed =
                await _context.Reviews.AnyAsync(r =>
                    r.ProductId == productId &&
                    r.UserId == userId);

            if (!alreadyReviewed &&
                rating >= 1 &&
                rating <= 5)
            {
                _context.Reviews.Add(new Review
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    IsApproved = false
                });

                await _context.SaveChangesAsync();

                TempData["ReviewSubmitted"] =
                    "Thanks! Your review has been submitted and will show once approved.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id = productId });
        }
    }
}