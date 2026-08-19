using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.SupportModule)]
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public ReviewsController(AppDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        // GET: /Admin/Reviews
        public async Task<IActionResult> Index(string? filter)
        {
            var query = _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            query = filter switch
            {
                "pending" => query.Where(r => !r.IsApproved),
                "approved" => query.Where(r => r.IsApproved),
                _ => query
            };

            var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            var list = reviews.Select(r => new ReviewListItemViewModel
            {
                Id = r.Id,
                ProductName = r.Product.Name,
                CustomerName = r.User.FullName,
                CustomerEmail = r.User.Email ?? "",
                Rating = r.Rating,
                Comment = r.Comment,
                IsApproved = r.IsApproved,
                CreatedAt = r.CreatedAt
            }).ToList();

            ViewBag.CurrentFilter = filter;
            ViewBag.PendingCount = await _context.Reviews.CountAsync(r => !r.IsApproved);
            ViewData["Title"] = "Reviews";
            return View(list);
        }

        // POST: /Admin/Reviews/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? filter)
        {
            var review = await _context.Reviews.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return NotFound();

            review.IsApproved = true;
            review.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Review", review.Id.ToString(), $"Approved review on {review.Product.Name}");

            TempData["Success"] = "Review approved.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // POST: /Admin/Reviews/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? filter)
        {
            var review = await _context.Reviews.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return NotFound();

            review.IsApproved = false;
            review.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Review", review.Id.ToString(), $"Unapproved/rejected review on {review.Product.Name}");

            TempData["Success"] = "Review hidden.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // POST: /Admin/Reviews/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? filter)
        {
            var review = await _context.Reviews.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return NotFound();

            await _auditLogger.LogAsync("Delete", "Review", review.Id.ToString(), $"Deleted review on {review.Product.Name}");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Review deleted.";
            return RedirectToAction(nameof(Index), new { filter });
        }
    }
}