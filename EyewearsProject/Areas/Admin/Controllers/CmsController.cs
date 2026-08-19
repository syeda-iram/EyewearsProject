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
    public class CmsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public CmsController(AppDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        // GET: /Admin/Cms
        public async Task<IActionResult> Index(string? type)
        {
            var query = _context.CmsContents.AsQueryable();

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<CmsPageType>(type, out var parsedType))
                query = query.Where(c => c.Type == parsedType);

            var items = await query.OrderBy(c => c.Type).ThenBy(c => c.SortOrder).ToListAsync();

            ViewBag.CurrentType = type;
            ViewData["Title"] = "CMS";
            return View(items);
        }

        // GET: /Admin/Cms/Create
        public IActionResult Create() => View(new CmsContentFormViewModel());

        // POST: /Admin/Cms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CmsContentFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var slugTaken = await _context.CmsContents.AnyAsync(c => c.Slug == model.Slug);
            if (slugTaken)
            {
                ModelState.AddModelError(nameof(model.Slug), "This slug is already in use.");
                return View(model);
            }

            var content = new CmsContent
            {
                Type = model.Type,
                Title = model.Title,
                Slug = model.Slug,
                Body = model.Body,
                ImageUrl = model.ImageUrl,
                LinkUrl = model.LinkUrl,
                IsActive = model.IsActive,
                SortOrder = model.SortOrder
            };

            _context.CmsContents.Add(content);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "CmsContent", content.Id.ToString(), $"Created {content.Type} '{content.Title}'");

            TempData["Success"] = "Content created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Cms/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var content = await _context.CmsContents.FindAsync(id);
            if (content == null) return NotFound();

            var model = new CmsContentFormViewModel
            {
                Id = content.Id,
                Type = content.Type,
                Title = content.Title,
                Slug = content.Slug,
                Body = content.Body,
                ImageUrl = content.ImageUrl,
                LinkUrl = content.LinkUrl,
                IsActive = content.IsActive,
                SortOrder = content.SortOrder
            };

            return View(model);
        }

        // POST: /Admin/Cms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CmsContentFormViewModel model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var content = await _context.CmsContents.FindAsync(id);
            if (content == null) return NotFound();

            var slugTaken = await _context.CmsContents.AnyAsync(c => c.Slug == model.Slug && c.Id != id);
            if (slugTaken)
            {
                ModelState.AddModelError(nameof(model.Slug), "This slug is already in use.");
                return View(model);
            }

            content.Type = model.Type;
            content.Title = model.Title;
            content.Slug = model.Slug;
            content.Body = model.Body;
            content.ImageUrl = model.ImageUrl;
            content.LinkUrl = model.LinkUrl;
            content.IsActive = model.IsActive;
            content.SortOrder = model.SortOrder;
            content.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Update", "CmsContent", content.Id.ToString(), $"Updated {content.Type} '{content.Title}'");

            TempData["Success"] = "Content updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Cms/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var content = await _context.CmsContents.FindAsync(id);
            if (content == null) return NotFound();

            content.IsActive = !content.IsActive;
            content.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(content.IsActive ? "Activate" : "Deactivate", "CmsContent", content.Id.ToString(), content.Title);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Cms/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var content = await _context.CmsContents.FindAsync(id);
            if (content == null) return NotFound();

            await _auditLogger.LogAsync("Delete", "CmsContent", content.Id.ToString(), content.Title);

            _context.CmsContents.Remove(content);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Content deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}