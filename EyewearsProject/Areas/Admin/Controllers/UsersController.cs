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

        private static readonly string[] RestrictedRoles =
        {
            AdminRoles.SuperAdmin,
            AdminRoles.Admin
        };

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IAuditLogger auditLogger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditLogger = auditLogger;
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private bool IsBlockedFromRole(string targetRole)
        {
            if (string.IsNullOrWhiteSpace(targetRole))
                return false;

            // SuperAdmin can manage all roles
            if (User.IsInRole(AdminRoles.SuperAdmin))
                return false;

            return RestrictedRoles.Contains(targetRole);
        }

        private List<string?> GetAssignableRoles()
        {
            if (User.IsInRole(AdminRoles.SuperAdmin))
            {
                return _roleManager.Roles
                    .Select(r => r.Name)
                    .ToList();
            }

            return _roleManager.Roles
                .Select(r => r.Name)
                .Where(r =>
                    !string.IsNullOrWhiteSpace(r) &&
                    !RestrictedRoles.Contains(r))
                .ToList();
        }

        private async Task<bool> CanManageUserAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            // SuperAdmin can manage everyone
            if (User.IsInRole(AdminRoles.SuperAdmin))
                return true;

            // Other admins/user managers cannot manage
            // restricted accounts.
            return !roles.Any(r => RestrictedRoles.Contains(r));
        }

        private IActionResult AccessDeniedUserManagement()
        {
            TempData["Error"] =
                "You are not permitted to modify this account.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // INDEX
        // =========================================================

        // GET: /Admin/Users
        public async Task<IActionResult> Index(
            string? search,
            string? role,
            string? status)
        {
            var query = _userManager.Users.AsQueryable();

            // -----------------------------------------------------
            // SEARCH
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    (u.Email != null && u.Email.Contains(search)));
            }

            // -----------------------------------------------------
            // STATUS FILTER
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.ToLower();

                if (status == "active")
                {
                    query = query.Where(u =>
                        u.IsActive &&
                        !u.IsDeleted);
                }
                else if (status == "inactive")
                {
                    query = query.Where(u =>
                        !u.IsActive &&
                        !u.IsDeleted);
                }
                else if (status == "deleted")
                {
                    query = query.Where(u =>
                        u.IsDeleted);
                }
            }

            var users = await query
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var list = new List<UserListItemViewModel>();

            foreach (var user in users)
            {
                var roles =
                    await _userManager.GetRolesAsync(user);

                // -------------------------------------------------
                // ROLE FILTER
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(role) &&
                    !roles.Contains(role))
                {
                    continue;
                }

                // -------------------------------------------------
                // NON-SUPERADMIN SECURITY
                // -------------------------------------------------

                if (!User.IsInRole(AdminRoles.SuperAdmin) &&
                    roles.Any(r => RestrictedRoles.Contains(r)))
                {
                    continue;
                }

                list.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    IsActive = user.IsActive,
                    IsDeleted = user.IsDeleted,
                    Roles = roles.ToList(),
                    CreatedAt = user.CreatedAt
                });
            }

            ViewBag.AllRoles = GetAssignableRoles();

            ViewBag.TotalUsers =
                await _userManager.Users.CountAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRole = role;
            ViewBag.CurrentStatus = status;

            ViewData["Title"] = "User Management";

            return View(list);
        }

        // =========================================================
        // BULK BLOCK
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkBlock(
            List<string> selectedIds)
        {
            var processed = 0;

            foreach (var id in selectedIds ?? new List<string>())
            {
                var user =
                    await _userManager.FindByIdAsync(id);

                if (user == null)
                    continue;

                // Deleted users cannot be modified
                if (user.IsDeleted)
                    continue;

                if (!await CanManageUserAsync(user))
                    continue;

                user.IsActive = false;

                var result =
                    await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                    continue;

                await _auditLogger.LogAsync(
                    "Deactivate",
                    "User",
                    user.Id,
                    $"{user.Email} (bulk block)");

                processed++;
            }

            TempData["Success"] =
                $"{processed} user(s) blocked.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // BULK CHANGE ROLE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkChangeRole(
            List<string> selectedIds,
            string newRole)
        {
            if (string.IsNullOrWhiteSpace(newRole))
            {
                TempData["Error"] =
                    "Please select a role.";

                return RedirectToAction(nameof(Index));
            }

            // Check target role permission
            if (IsBlockedFromRole(newRole))
            {
                TempData["Error"] =
                    "You are not permitted to assign that role.";

                return RedirectToAction(nameof(Index));
            }

            // Make sure role actually exists
            var roleExists =
                await _roleManager.RoleExistsAsync(newRole);

            if (!roleExists)
            {
                TempData["Error"] =
                    "The selected role does not exist.";

                return RedirectToAction(nameof(Index));
            }

            var processed = 0;

            foreach (var id in selectedIds ?? new List<string>())
            {
                var user =
                    await _userManager.FindByIdAsync(id);

                if (user == null)
                    continue;

                // Deleted users cannot have roles changed
                if (user.IsDeleted)
                    continue;

                if (!await CanManageUserAsync(user))
                    continue;

                var currentRoles =
                    await _userManager.GetRolesAsync(user);

                // A non-superadmin cannot touch restricted roles
                if (!User.IsInRole(AdminRoles.SuperAdmin) &&
                    currentRoles.Any(r =>
                        RestrictedRoles.Contains(r)))
                {
                    continue;
                }

                if (currentRoles.Any())
                {
                    var removeResult =
                        await _userManager.RemoveFromRolesAsync(
                            user,
                            currentRoles);

                    if (!removeResult.Succeeded)
                        continue;
                }

                var addResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        newRole);

                if (!addResult.Succeeded)
                    continue;

                await _auditLogger.LogAsync(
                    "Update",
                    "User",
                    user.Id,
                    $"{user.Email} role changed to {newRole} (bulk)");

                processed++;
            }

            TempData["Success"] =
                $"Role updated to {newRole} for {processed} eligible user(s).";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // BULK RESET PASSWORD
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkResetPassword(
            List<string> selectedIds)
        {
            var results = new List<string>();

            foreach (var id in selectedIds ?? new List<string>())
            {
                var user =
                    await _userManager.FindByIdAsync(id);

                if (user == null)
                    continue;

                // Deleted users cannot have password reset
                if (user.IsDeleted)
                    continue;

                if (!await CanManageUserAsync(user))
                    continue;

                var tempPassword =
                    "Temp@" +
                    Guid.NewGuid()
                        .ToString("N")[..8];

                var token =
                    await _userManager
                        .GeneratePasswordResetTokenAsync(user);

                var reset =
                    await _userManager.ResetPasswordAsync(
                        user,
                        token,
                        tempPassword);

                if (!reset.Succeeded)
                    continue;

                results.Add(
                    $"{user.Email}: {tempPassword}");

                await _auditLogger.LogAsync(
                    "Update",
                    "User",
                    user.Id,
                    $"Password reset for {user.Email}");
            }

            TempData["ResetResults"] =
                string.Join(" | ", results);

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        public IActionResult Create()
        {
            ViewBag.AllRoles =
                GetAssignableRoles();

            return View();
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            UserCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Please select a role.");
            }
            else if (IsBlockedFromRole(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "You are not permitted to create a user with that role.");
            }
            else if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "The selected role does not exist.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AllRoles =
                    GetAssignableRoles();

                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,

                // Admin-created users are already verified
                EmailConfirmed = true,

                IsActive = true,
                IsDeleted = false
            };

            var result =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                ViewBag.AllRoles =
                    GetAssignableRoles();

                return View(model);
            }

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    model.Role);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                ViewBag.AllRoles =
                    GetAssignableRoles();

                return View(model);
            }

            await _auditLogger.LogAsync(
                "Create",
                "User",
                user.Id,
                $"Created {user.Email} as {model.Role}");

            TempData["Success"] =
                "User created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        public async Task<IActionResult> Edit(string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // Deleted users must be restored first
            if (user.IsDeleted)
            {
                TempData["Error"] =
                    "Deleted users cannot be edited. Restore the user first.";

                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageUserAsync(user))
                return AccessDeniedUserManagement();

            var roles =
                await _userManager.GetRolesAsync(user);

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                IsActive = user.IsActive,
                SelectedRole =
                    roles.FirstOrDefault() ?? ""
            };

            ViewBag.AllRoles =
                GetAssignableRoles();

            return View(model);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            UserEditViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // Deleted users cannot be edited
            if (user.IsDeleted)
            {
                TempData["Error"] =
                    "Deleted users cannot be edited. Restore the user first.";

                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageUserAsync(user))
                return AccessDeniedUserManagement();

            if (IsBlockedFromRole(model.SelectedRole))
            {
                TempData["Error"] =
                    "You are not permitted to assign that role.";

                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(model.SelectedRole) &&
                !await _roleManager.RoleExistsAsync(model.SelectedRole))
            {
                ModelState.AddModelError(
                    nameof(model.SelectedRole),
                    "The selected role does not exist.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AllRoles =
                    GetAssignableRoles();

                return View(model);
            }

            var currentRoles =
                await _userManager.GetRolesAsync(user);

            // -----------------------------------------------------
            // Update basic information
            // -----------------------------------------------------

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.IsActive = model.IsActive;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                ViewBag.AllRoles =
                    GetAssignableRoles();

                return View(model);
            }

            // -----------------------------------------------------
            // Update role
            // -----------------------------------------------------

            if (currentRoles.Any())
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(
                        user,
                        currentRoles);

                if (!removeResult.Succeeded)
                {
                    TempData["Error"] =
                        "User information was updated, but the role could not be changed.";

                    return RedirectToAction(nameof(Index));
                }
            }

            if (!string.IsNullOrWhiteSpace(model.SelectedRole))
            {
                var addResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        model.SelectedRole);

                if (!addResult.Succeeded)
                {
                    TempData["Error"] =
                        "User information was updated, but the new role could not be assigned.";

                    return RedirectToAction(nameof(Index));
                }
            }

            await _auditLogger.LogAsync(
                "Update",
                "User",
                user.Id,
                $"Updated {user.Email}, role set to {model.SelectedRole}");

            TempData["Success"] =
                "User updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // TOGGLE ACTIVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(
            string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // Deleted users cannot be activated/deactivated
            if (user.IsDeleted)
            {
                TempData["Error"] =
                    "Deleted users cannot be activated or deactivated. Restore the user first.";

                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageUserAsync(user))
                return AccessDeniedUserManagement();

            user.IsActive = !user.IsActive;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Unable to update the user's status.";

                return RedirectToAction(nameof(Index));
            }

            await _auditLogger.LogAsync(
                user.IsActive
                    ? "Activate"
                    : "Deactivate",
                "User",
                user.Id,
                user.Email);

            TempData["Success"] =
                user.IsActive
                    ? "User activated successfully."
                    : "User deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // SOFT DELETE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // Already deleted
            if (user.IsDeleted)
            {
                TempData["Error"] =
                    "This user has already been deleted.";

                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageUserAsync(user))
                return AccessDeniedUserManagement();

            var roles =
                await _userManager.GetRolesAsync(user);

            // Soft delete
            user.IsDeleted = true;

            // Deleted users cannot log in
            user.IsActive = false;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Unable to delete this user.";

                return RedirectToAction(nameof(Index));
            }

            await _auditLogger.LogAsync(
                "Delete",
                "User",
                user.Id,
                $"{user.Email} | Roles: {string.Join(", ", roles)}");

            TempData["Success"] =
                "User deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // RESTORE USER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(
            string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // Only deleted users can be restored
            if (!user.IsDeleted)
            {
                TempData["Error"] =
                    "This user is not deleted.";

                return RedirectToAction(nameof(Index));
            }

            // Security check
            if (!await CanManageUserAsync(user))
                return AccessDeniedUserManagement();

            // Restore user
            user.IsDeleted = false;

            // Restored users become active
            user.IsActive = true;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Unable to restore this user.";

                return RedirectToAction(nameof(Index));
            }

            await _auditLogger.LogAsync(
                "Restore",
                "User",
                user.Id,
                user.Email);

            TempData["Success"] =
                "User restored successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}