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
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public CategoriesController(AppDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.Include(c => c.ParentCategory).ToListAsync();
            ViewData["Title"] = "Categories";
            return View(categories);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateParentsAsync();
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            ModelState.Remove("ParentCategory"); // nav prop isn't posted from the form
            if (!ModelState.IsValid)
            {
                await PopulateParentsAsync();
                return View(model);
            }

            _context.Categories.Add(model);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "Category", model.Id.ToString(), $"Created category {model.Name}");

            TempData["Success"] = "Category created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            await PopulateParentsAsync(id);
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.Id) return NotFound();
            ModelState.Remove("ParentCategory");
            if (!ModelState.IsValid)
            {
                await PopulateParentsAsync(id);
                return View(model);
            }

            _context.Update(model);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Category", model.Id.ToString(), $"Updated category {model.Name}");

            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // log before removing — after Remove+SaveChanges, category.Name is gone
            await _auditLogger.LogAsync("Delete", "Category", category.Id.ToString(), category.Name);

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateParentsAsync(int? excludeId = null)
        {
            var query = _context.Categories.AsQueryable();
            if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
            ViewBag.ParentCategories = new SelectList(await query.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        }
    }
}