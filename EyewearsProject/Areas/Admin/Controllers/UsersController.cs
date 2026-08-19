using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EyewearsProject.Services;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.UserManagers)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAuditLogger _auditLogger;

        private static readonly string[] RestrictedRoles = { AdminRoles.SuperAdmin, AdminRoles.Admin };

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IAuditLogger auditLogger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditLogger = auditLogger;
        }

        private bool IsBlockedFromRole(string targetRole)
        {
            if (User.IsInRole(AdminRoles.SuperAdmin)) return false;
            return RestrictedRoles.Contains(targetRole);
        }

        private List<string?> GetAssignableRoles()
        {
            return User.IsInRole(AdminRoles.SuperAdmin)
                ? _roleManager.Roles.Select(r => r.Name).ToList()
                : _roleManager.Roles.Select(r => r.Name).Where(r => !RestrictedRoles.Contains(r!)).ToList();
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Index(string? search, string? role, string? status)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(u => status == "active" ? u.IsActive : !u.IsActive);

            var users = query.OrderBy(u => u.FullName).ToList();
            var list = new List<UserListItemViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role)) continue;

                list.Add(new UserListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    IsActive = u.IsActive,
                    Roles = roles.ToList(),
                    CreatedAt = u.CreatedAt
                });
            }

            if (!User.IsInRole(AdminRoles.SuperAdmin))
            {
                list = list.Where(u => !u.Roles.Any(r => RestrictedRoles.Contains(r))).ToList();
            }

            ViewBag.AllRoles = GetAssignableRoles();
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRole = role;
            ViewBag.CurrentStatus = status;
            ViewData["Title"] = "User Management";
            return View(list);
        }

        // POST: /Admin/Users/BulkBlock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkBlock(List<string> selectedIds)
        {
            foreach (var id in selectedIds ?? new())
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) continue;

                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Any(r => IsBlockedFromRole(r))) continue;

                user.IsActive = false;
                await _userManager.UpdateAsync(user);

                await _auditLogger.LogAsync("Deactivate", "User", user.Id, $"{user.Email} (bulk block)");
            }
            TempData["Success"] = $"{selectedIds?.Count ?? 0} user(s) processed.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Users/BulkChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkChangeRole(List<string> selectedIds, string newRole)
        {
            if (string.IsNullOrEmpty(newRole)) return RedirectToAction(nameof(Index));

            if (IsBlockedFromRole(newRole))
            {
                TempData["Error"] = "You are not permitted to assign that role.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var id in selectedIds ?? new())
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) continue;

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any(r => IsBlockedFromRole(r))) continue;

                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, newRole);

                await _auditLogger.LogAsync("Update", "User", user.Id, $"{user.Email} role changed to {newRole} (bulk)");
            }
            TempData["Success"] = $"Role updated to {newRole} for eligible user(s).";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Users/BulkResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkResetPassword(List<string> selectedIds)
        {
            var results = new List<string>();
            foreach (var id in selectedIds ?? new())
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) continue;

                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Any(r => IsBlockedFromRole(r))) continue;

                var tempPassword = "Temp@" + Guid.NewGuid().ToString("N")[..8];
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var reset = await _userManager.ResetPasswordAsync(user, token, tempPassword);

                if (reset.Succeeded)
                {
                    results.Add($"{user.Email}: {tempPassword}");
                    await _auditLogger.LogAsync("Update", "User", user.Id, $"Password reset for {user.Email}");
                }
            }
            TempData["ResetResults"] = string.Join(" | ", results);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Users/Create
        public IActionResult Create()
        {
            ViewBag.AllRoles = GetAssignableRoles();
            return View();
        }

        // POST: /Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (IsBlockedFromRole(model.Role))
            {
                ModelState.AddModelError("", "You are not permitted to create a user with that role.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AllRoles = GetAssignableRoles();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                ViewBag.AllRoles = GetAssignableRoles();
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditLogger.LogAsync("Create", "User", user.Id, $"Created {user.Email} as {model.Role}");

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Users/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Any(r => IsBlockedFromRole(r)))
            {
                TempData["Error"] = "You are not permitted to edit this account.";
                return RedirectToAction(nameof(Index));
            }

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                IsActive = user.IsActive,
                SelectedRole = roles.FirstOrDefault() ?? ""
            };

            ViewBag.AllRoles = GetAssignableRoles();
            return View(model);
        }

        // POST: /Admin/Users/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditViewModel model)
        {
            if (id != model.Id) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any(r => IsBlockedFromRole(r)) || IsBlockedFromRole(model.SelectedRole))
            {
                TempData["Error"] = "You are not permitted to edit this account or assign that role.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AllRoles = GetAssignableRoles();
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.IsActive = model.IsActive;
            await _userManager.UpdateAsync(user);

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!string.IsNullOrEmpty(model.SelectedRole))
                await _userManager.AddToRoleAsync(user, model.SelectedRole);

            await _auditLogger.LogAsync("Update", "User", user.Id, $"Updated {user.Email}, role set to {model.SelectedRole}");

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Users/ToggleActive/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => IsBlockedFromRole(r)))
            {
                TempData["Error"] = "You are not permitted to modify this account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditLogger.LogAsync(user.IsActive ? "Activate" : "Deactivate", "User", user.Id, user.Email);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Users/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => IsBlockedFromRole(r)))
            {
                TempData["Error"] = "You are not permitted to delete this account.";
                return RedirectToAction(nameof(Index));
            }

            await _auditLogger.LogAsync("Delete", "User", user.Id, user.Email);

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "User deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}