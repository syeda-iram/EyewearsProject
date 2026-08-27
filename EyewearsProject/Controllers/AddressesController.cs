using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Controllers
{
    [Authorize]
    public class AddressesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AddressesController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // INDEX
        // =========================================================

        // GET: /Addresses
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(addresses);
        }

        // =========================================================
        // CREATE
        // =========================================================

        // GET: /Addresses/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Address());
        }

        // POST: /Addresses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Address address)
        {
            if (!ModelState.IsValid)
            {
                return View(address);
            }

            var userId = _userManager.GetUserId(User)!;

            address.UserId = userId;
            address.CreatedAt = DateTime.UtcNow;
            address.UpdatedAt = null;

            // Check if the customer already has an address
            bool hasAnyAddress = await _context.Addresses
                .AnyAsync(a => a.UserId == userId);

            // First address automatically becomes default
            if (!hasAnyAddress)
            {
                address.IsDefault = true;
            }

            // If this address is selected as default,
            // remove default from all other addresses.
            if (address.IsDefault)
            {
                await ClearOtherDefaultsAsync(userId);
            }

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT
        // =========================================================

        // GET: /Addresses/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            return View(address);
        }

        // POST: /Addresses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Address updated)
        {
            var userId = _userManager.GetUserId(User)!;

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                updated.Id = address.Id;
                updated.UserId = address.UserId;

                return View(updated);
            }

            // Update fields
            address.AddressType = updated.AddressType;
            address.FullName = updated.FullName;
            address.Phone = updated.Phone;
            address.AddressLine1 = updated.AddressLine1;
            address.AddressLine2 = updated.AddressLine2;
            address.City = updated.City;
            address.State = updated.State;
            address.PostalCode = updated.PostalCode;
            address.Country = updated.Country;
            address.UpdatedAt = DateTime.UtcNow;

            // Default handling
            if (updated.IsDefault)
            {
                await ClearOtherDefaultsAsync(userId);

                address.IsDefault = true;
            }
            else
            {
                address.IsDefault = false;

                // Make sure the customer always has
                // a default address if other addresses exist.
                bool hasAnotherDefault = await _context.Addresses
                    .AnyAsync(a =>
                        a.UserId == userId &&
                        a.Id != address.Id &&
                        a.IsDefault);

                if (!hasAnotherDefault)
                {
                    address.IsDefault = true;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE
        // =========================================================

        // POST: /Addresses/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            bool wasDefault = address.IsDefault;

            _context.Addresses.Remove(address);

            await _context.SaveChangesAsync();

            // If the deleted address was default,
            // automatically select another one.
            if (wasDefault)
            {
                var newDefault = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (newDefault != null)
                {
                    newDefault.IsDefault = true;
                    newDefault.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // SET DEFAULT
        // =========================================================

        // POST: /Addresses/SetDefault/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            await ClearOtherDefaultsAsync(userId);

            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // HELPER
        // =========================================================

        private async Task ClearOtherDefaultsAsync(string userId)
        {
            var defaultAddresses = await _context.Addresses
                .Where(a =>
                    a.UserId == userId &&
                    a.IsDefault)
                .ToListAsync();

            foreach (var address in defaultAddresses)
            {
                address.IsDefault = false;
                address.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}