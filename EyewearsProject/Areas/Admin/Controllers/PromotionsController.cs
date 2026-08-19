using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.MarketingModule)]
    public class PromotionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public PromotionsController(AppDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        // GET: /Admin/Promotions
        public async Task<IActionResult> Index()
        {
            var promotions = await _context.Promotions.OrderByDescending(p => p.Id).ToListAsync();
            ViewData["Title"] = "Promotions";
            return View(promotions);
        }

        // GET: /Admin/Promotions/Create
        public IActionResult Create() => View(new PromotionFormViewModel());

        // POST: /Admin/Promotions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionFormViewModel model)
        {
            if (model.EndDate < model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "End date must be after start date.");

            if (!ModelState.IsValid) return View(model);

            var exists = await _context.Promotions.AnyAsync(p => p.Code == model.Code);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Code), "This coupon code already exists.");
                return View(model);
            }

            var promo = new Promotion
            {
                Code = model.Code.ToUpper(),
                Description = model.Description,
                DiscountType = model.DiscountType,
                DiscountValue = model.DiscountValue,
                MinOrderAmount = model.MinOrderAmount,
                MaxDiscountAmount = model.MaxDiscountAmount,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                UsageLimit = model.UsageLimit,
                IsActive = model.IsActive
            };

            _context.Promotions.Add(promo);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "Promotion", promo.Id.ToString(), $"Created coupon {promo.Code}");

            TempData["Success"] = "Promotion created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Promotions/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            var model = new PromotionFormViewModel
            {
                Id = promo.Id,
                Code = promo.Code,
                Description = promo.Description,
                DiscountType = promo.DiscountType,
                DiscountValue = promo.DiscountValue,
                MinOrderAmount = promo.MinOrderAmount,
                MaxDiscountAmount = promo.MaxDiscountAmount,
                StartDate = promo.StartDate,
                EndDate = promo.EndDate,
                UsageLimit = promo.UsageLimit,
                IsActive = promo.IsActive
            };

            return View(model);
        }

        // POST: /Admin/Promotions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromotionFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (model.EndDate < model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "End date must be after start date.");

            if (!ModelState.IsValid) return View(model);

            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            var codeTaken = await _context.Promotions.AnyAsync(p => p.Code == model.Code && p.Id != id);
            if (codeTaken)
            {
                ModelState.AddModelError(nameof(model.Code), "This coupon code already exists.");
                return View(model);
            }

            promo.Code = model.Code.ToUpper();
            promo.Description = model.Description;
            promo.DiscountType = model.DiscountType;
            promo.DiscountValue = model.DiscountValue;
            promo.MinOrderAmount = model.MinOrderAmount;
            promo.MaxDiscountAmount = model.MaxDiscountAmount;
            promo.StartDate = model.StartDate;
            promo.EndDate = model.EndDate;
            promo.UsageLimit = model.UsageLimit;
            promo.IsActive = model.IsActive;
            promo.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Promotion", promo.Id.ToString(), $"Updated coupon {promo.Code}");

            TempData["Success"] = "Promotion updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Promotions/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            promo.IsActive = !promo.IsActive;
            promo.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(promo.IsActive ? "Activate" : "Deactivate", "Promotion", promo.Id.ToString(), promo.Code);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Promotions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            await _auditLogger.LogAsync("Delete", "Promotion", promo.Id.ToString(), promo.Code);

            _context.Promotions.Remove(promo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Promotion deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}