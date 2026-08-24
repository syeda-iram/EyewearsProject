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

        public ProductsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Products
        public async Task<IActionResult> Index(string? category, int? brandId, string? color,
    decimal? minPrice, decimal? maxPrice, string? spec)
        {
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Include(p => p.Specifications)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.Name == category);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            if (!string.IsNullOrWhiteSpace(color))
                query = query.Where(p => p.Variants.Any(v => v.Color == color));

            if (minPrice.HasValue)
                query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(spec))
            {
                // spec comes in as "Name:Value", e.g. "Material:Acetate"
                var parts = spec.Split(':', 2);
                if (parts.Length == 2)
                {
                    var specName = parts[0];
                    var specValue = parts[1];
                    query = query.Where(p => p.Specifications.Any(s => s.Name == specName && s.Value == specValue));
                }
            }

            var products = await query.ToListAsync();

            // Facet data for the filter sidebar — pulled from the FULL catalog,
            // not the filtered result, so options don't disappear as you filter.
            ViewBag.AllBrands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
            ViewBag.AllColors = await _context.ProductVariants
                .Select(v => v.Color)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            ViewBag.AllSpecs = await _context.ProductSpecifications
                .Select(s => new { s.Name, s.Value })
                .Distinct()
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.SelectedCategory = category;
            ViewBag.SelectedBrandId = brandId;
            ViewBag.SelectedColor = color;
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
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var approvedReviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.ApprovedReviews = approvedReviews;
            ViewBag.AverageRating = approvedReviews.Any() ? approvedReviews.Average(r => r.Rating) : (double?)null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.UserAlreadyReviewed = await _context.Reviews
                    .AnyAsync(r => r.ProductId == id && r.UserId == userId);
            }

            return View(product);
        }

        // POST: /Products/SubmitReview
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string? comment)
        {
            var userId = _userManager.GetUserId(User)!;

            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.ProductId == productId && r.UserId == userId);

            if (!alreadyReviewed && rating >= 1 && rating <= 5)
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
                TempData["ReviewSubmitted"] = "Thanks! Your review has been submitted and will show once approved.";
            }

            return RedirectToAction("Details", new { id = productId });
        }
    }
}