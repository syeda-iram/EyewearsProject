using EyewearsProject.Areas.Admin.Models;
using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.VendorModule)]
    public class VendorsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogger _auditLogger;

        public VendorsController(AppDbContext context, UserManager<ApplicationUser> userManager, IAuditLogger auditLogger)
        {
            _context = context;
            _userManager = userManager;
            _auditLogger = auditLogger;
        }

        // GET: /Admin/Vendors
        public async Task<IActionResult> Index()
        {
            var vendors = await _context.Vendors
                .Include(v => v.PurchaseOrders)
                .OrderBy(v => v.CompanyName)
                .ToListAsync();

            ViewData["Title"] = "Vendors";
            return View(vendors);
        }

        // GET: /Admin/Vendors/Create
        public IActionResult Create() => View(new VendorFormViewModel());

        // POST: /Admin/Vendors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
                ModelState.AddModelError(nameof(model.Password), "A login password (min 6 characters) is required to create the vendor's account.");

            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.ContactEmail);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.ContactEmail), "A user with this email already exists.");
                return View(model);
            }

            // Create the login account for this vendor first
            var user = new ApplicationUser
            {
                UserName = model.ContactEmail,
                Email = model.ContactEmail,
                FullName = model.ContactName,
                EmailConfirmed = true,
                IsActive = model.IsActive
            };

            var result = await _userManager.CreateAsync(user, model.Password!);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Vendor");

            var vendor = new EyewearsProject.Models.Vendor
            {
                CompanyName = model.CompanyName,
                ContactName = model.ContactName,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone,
                Address = model.Address,
                VendorType = model.VendorType,
                IsActive = model.IsActive,
                UserId = user.Id
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync("Create", "Vendor", vendor.Id.ToString(), $"Created vendor {vendor.CompanyName} with login {model.ContactEmail}");

            TempData["Success"] = "Vendor created — they can now log in at the Vendor Portal.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Vendors/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();

            var model = new VendorFormViewModel
            {
                Id = vendor.Id,
                CompanyName = vendor.CompanyName,
                ContactName = vendor.ContactName,
                ContactEmail = vendor.ContactEmail,
                ContactPhone = vendor.ContactPhone,
                Address = vendor.Address,
                VendorType = vendor.VendorType,
                IsActive = vendor.IsActive
            };

            return View(model);
        }

        // POST: /Admin/Vendors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VendorFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();

            // Password field is optional on Edit — only validate if they're changing it
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < 6)
                ModelState.AddModelError(nameof(model.Password), "Password must be at least 6 characters.");

            if (!ModelState.IsValid) return View(model);

            vendor.CompanyName = model.CompanyName;
            vendor.ContactName = model.ContactName;
            vendor.ContactPhone = model.ContactPhone;
            vendor.Address = model.Address;
            vendor.VendorType = model.VendorType;
            vendor.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            // Keep the login account's active status and name in sync with the vendor record
            var user = await _userManager.FindByIdAsync(vendor.UserId);
            if (user != null)
            {
                user.FullName = model.ContactName;
                user.IsActive = model.IsActive;
                await _userManager.UpdateAsync(user);

                if (!string.IsNullOrEmpty(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, token, model.Password);
                }
            }

            await _auditLogger.LogAsync("Update", "Vendor", vendor.Id.ToString(), $"Updated vendor {vendor.CompanyName}");

            TempData["Success"] = "Vendor updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Vendors/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();

            vendor.IsActive = !vendor.IsActive;
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(vendor.UserId);
            if (user != null)
            {
                user.IsActive = vendor.IsActive;
                await _userManager.UpdateAsync(user);
            }

            await _auditLogger.LogAsync(vendor.IsActive ? "Activate" : "Deactivate", "Vendor", vendor.Id.ToString(), vendor.CompanyName);

            return RedirectToAction(nameof(Index));
        }
    }
}