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
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .ToListAsync();

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