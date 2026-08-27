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

        // GET: /Products
        public async Task<IActionResult> Index(
            string? category,
            int? brandId,
            string? color,
            string? productType,
            string? gender,
            string? material,
            string? shape,
            string? tag,
            decimal? minPrice,
            decimal? maxPrice,
            string? spec)
        {
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Include(p => p.Specifications)
                .Include(p => p.ProductTags)
                .Where(p => p.IsActive)
                .AsQueryable();

            // Category
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p =>
                    p.Category.Name == category);
            }

            // Brand
            if (brandId.HasValue)
            {
                query = query.Where(p =>
                    p.BrandId == brandId.Value);
            }

            // Color
            // Check Product.Color first and then Variant.Color.
            if (!string.IsNullOrWhiteSpace(color))
            {
                query = query.Where(p =>
                    p.Color == color ||
                    p.Variants.Any(v => v.Color == color));
            }

            // Product Type
            if (!string.IsNullOrWhiteSpace(productType))
            {
                query = query.Where(p =>
                    p.ProductType == productType);
            }

            // Gender
            if (!string.IsNullOrWhiteSpace(gender))
            {
                query = query.Where(p =>
                    p.Gender == gender);
            }

            // Material
            if (!string.IsNullOrWhiteSpace(material))
            {
                query = query.Where(p =>
                    p.Material == material);
            }

            // Shape
            if (!string.IsNullOrWhiteSpace(shape))
            {
                query = query.Where(p =>
                    p.Shape == shape);
            }

            // Product Tag
            if (!string.IsNullOrWhiteSpace(tag))
            {
                query = query.Where(p =>
                    p.ProductTags.Any(t => t.Name == tag));
            }

            // Price
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

            // Dynamic Specification
            // Example:
            // Material:Acetate
            if (!string.IsNullOrWhiteSpace(spec))
            {
                var parts = spec.Split(':', 2);

                if (parts.Length == 2)
                {
                    var specName = parts[0];
                    var specValue = parts[1];

                    query = query.Where(p =>
                        p.Specifications.Any(s =>
                            s.Name == specName &&
                            s.Value == specValue));
                }
            }

            // Featured products first,
            // then newest products.
            var products = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            // ---------------------------------------
            // FILTER / FACET DATA
            // ---------------------------------------

            // Brands
            ViewBag.AllBrands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();

            // Colors from Product.Color
            var productColors = await _context.Products
                .Where(p =>
                    p.IsActive &&
                    p.Color != null &&
                    p.Color != "")
                .Select(p => p.Color!)
                .Distinct()
                .ToListAsync();

            // Colors from ProductVariant.Color
            var variantColors = await _context.ProductVariants
                .Where(v =>
                    v.Color != null &&
                    v.Color != "")
                .Select(v => v.Color)
                .Distinct()
                .ToListAsync();

            ViewBag.AllColors = productColors
                .Union(variantColors)
                .OrderBy(c => c)
                .ToList();

            // Product Types
            ViewBag.AllProductTypes = await _context.Products
                .Where(p =>
                    p.IsActive &&
                    p.ProductType != null &&
                    p.ProductType != "")
                .Select(p => p.ProductType!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Genders
            ViewBag.AllGenders = await _context.Products
                .Where(p =>
                    p.IsActive &&
                    p.Gender != null &&
                    p.Gender != "")
                .Select(p => p.Gender!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Materials
            ViewBag.AllMaterials = await _context.Products
                .Where(p =>
                    p.IsActive &&
                    p.Material != null &&
                    p.Material != "")
                .Select(p => p.Material!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Shapes
            ViewBag.AllShapes = await _context.Products
                .Where(p =>
                    p.IsActive &&
                    p.Shape != null &&
                    p.Shape != "")
                .Select(p => p.Shape!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Product Tags
            ViewBag.AllTags = await _context.ProductTags
                .Where(t =>
                    t.Product.IsActive &&
                    t.Name != null &&
                    t.Name != "")
                .Select(t => t.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // Dynamic Specifications
            ViewBag.AllSpecs = await _context.ProductSpecifications
                .Where(s => s.Product.IsActive)
                .Select(s => new
                {
                    s.Name,
                    s.Value
                })
                .Distinct()
                .OrderBy(s => s.Name)
                .ToListAsync();

            // ---------------------------------------
            // SELECTED FILTERS
            // ---------------------------------------

            ViewBag.SelectedCategory = category;
            ViewBag.SelectedBrandId = brandId;
            ViewBag.SelectedColor = color;
            ViewBag.SelectedProductType = productType;
            ViewBag.SelectedGender = gender;
            ViewBag.SelectedMaterial = material;
            ViewBag.SelectedShape = shape;
            ViewBag.SelectedTag = tag;
            ViewBag.SelectedMinPrice = minPrice;
            ViewBag.SelectedMaxPrice = maxPrice;
            ViewBag.SelectedSpec = spec;

            return View(products);
        }

        // GET: /Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                    .ThenInclude(i => i.ProductVariant)
                .Include(p => p.Variants)
                .Include(p => p.Specifications)
                .Include(p => p.ProductTags)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.IsActive);

            if (product == null)
                return NotFound();

            // Approved reviews
            var approvedReviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r =>
                    r.ProductId == id &&
                    r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.ApprovedReviews = approvedReviews;

            // Average rating
            ViewBag.AverageRating =
                approvedReviews.Any()
                    ? approvedReviews.Average(r => r.Rating)
                    : (double?)null;

            // Check whether current user already reviewed
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

        // POST: /Products/SubmitReview
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