using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.ProductsModule)]
    public class BrandsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public BrandsController(AppDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Brands";
            return View(await _context.Brands.ToListAsync());
        }

        public IActionResult Create() => View(new Brand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Brands.Add(model);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "Brand", model.Id.ToString(), $"Created brand {model.Name}");

            TempData["Success"] = "Brand created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            _context.Update(model);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "Brand", model.Id.ToString(), $"Updated brand {model.Name}");

            TempData["Success"] = "Brand updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            // log before removing — after Remove+SaveChanges, brand.Name is gone
            await _auditLogger.LogAsync("Delete", "Brand", brand.Id.ToString(), brand.Name);

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Brand deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}