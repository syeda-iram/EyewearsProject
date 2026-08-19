using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.UserManagers)]
    public class RolesController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAuditLogger _auditLogger;

        private static readonly string[] ProtectedRoles = { AdminRoles.SuperAdmin, AdminRoles.Admin };

        public RolesController(RoleManager<ApplicationRole> roleManager, IAuditLogger auditLogger)
        {
            _roleManager = roleManager;
            _auditLogger = auditLogger;
        }

        // GET: /Admin/Roles
        public IActionResult Index()
        {
            var roles = _roleManager.Roles.OrderBy(r => r.Name).ToList();
            ViewData["Title"] = "Roles";
            return View(roles);
        }

        // GET: /Admin/Roles/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "New Role";
            return View();
        }

        // POST: /Admin/Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName, string? description)
        {
            ViewData["Title"] = "New Role";

            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError("", "Role name is required.");
                return View();
            }

            roleName = roleName.Trim();

            var exists = await _roleManager.RoleExistsAsync(roleName);
            if (exists)
            {
                ModelState.AddModelError("", $"Role '{roleName}' already exists.");
                return View();
            }

            // Block names that only differ by spacing/casing from a protected role —
            // prevents someone creating "Super Admin" or "superadmin" as a decoy of the real "SuperAdmin"
            var normalizedNew = roleName.Replace(" ", "").ToLowerInvariant();
            var collidesWithProtected = ProtectedRoles.Any(p => p.Replace(" ", "").ToLowerInvariant() == normalizedNew);

            if (collidesWithProtected)
            {
                ModelState.AddModelError("", $"'{roleName}' is too similar to a protected role name and cannot be used.");
                return View();
            }

            var role = new ApplicationRole { Name = roleName, Description = description };
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View();
            }

            await _auditLogger.LogAsync("Create", "Role", role.Id, $"Created role {role.Name}");

            TempData["Success"] = $"Role '{roleName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Roles/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            ViewData["Title"] = "Edit Role";
            return View(role);
        }

        // POST: /Admin/Roles/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string roleName, string? description)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            ViewData["Title"] = "Edit Role";

            if (ProtectedRoles.Contains(role.Name) && roleName != role.Name)
            {
                ModelState.AddModelError("", "This role's name is protected and cannot be changed.");
                return View(role);
            }

            role.Name = roleName;
            role.Description = description;
            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(role);
            }

            await _auditLogger.LogAsync("Update", "Role", role.Id, $"Updated role {role.Name}");

            TempData["Success"] = "Role updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Roles/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            if (ProtectedRoles.Contains(role.Name))
            {
                TempData["Error"] = $"'{role.Name}' is a protected role and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            await _auditLogger.LogAsync("Delete", "Role", role.Id, $"Deleted role {role.Name}");

            await _roleManager.DeleteAsync(role);
            TempData["Success"] = $"Role '{role.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}