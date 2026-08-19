using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.FullAccess)]
    public class AuditLogsController : Controller
    {
        private readonly AppDbContext _context;
        public AuditLogsController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index(string? entityType, string? search)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.UserEmail.Contains(search) || (a.Details != null && a.Details.Contains(search)));

            var logs = await query.OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync();

            ViewBag.EntityTypes = await _context.AuditLogs.Select(a => a.EntityType).Distinct().ToListAsync();
            ViewBag.CurrentEntityType = entityType;
            ViewBag.CurrentSearch = search;
            ViewData["Title"] = "Audit Logs";
            return View(logs);
        }
    }
}