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

        public AddressesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

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

        // GET: /Addresses/Create
        public IActionResult Create()
        {
            return View(new Address());
        }

        // POST: /Addresses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Address address)
        {
            if (!ModelState.IsValid) return View(address);

            var userId = _userManager.GetUserId(User)!;
            address.UserId = userId;
            address.CreatedAt = DateTime.UtcNow;

            // If this is the user's first address, or they explicitly marked it
            // default, make sure only one address stays default at a time.
            bool hasAnyAddress = await _context.Addresses.AnyAsync(a => a.UserId == userId);
            if (!hasAnyAddress) address.IsDefault = true;

            if (address.IsDefault)
            {
                await ClearOtherDefaultsAsync(userId);
            }

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: /Addresses/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null) return NotFound();

            return View(address);
        }

        // POST: /Addresses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Address updated)
        {
            var userId = _userManager.GetUserId(User)!;
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null) return NotFound();
            if (!ModelState.IsValid) return View(updated);

            address.Label = updated.Label;
            address.FullName = updated.FullName;
            address.Phone = updated.Phone;
            address.AddressLine = updated.AddressLine;
            address.City = updated.City;
            address.PostalCode = updated.PostalCode;
            address.Country = updated.Country;
            address.UpdatedAt = DateTime.UtcNow;

            if (updated.IsDefault && !address.IsDefault)
            {
                await ClearOtherDefaultsAsync(userId);
                address.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // POST: /Addresses/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null) return NotFound();

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // POST: /Addresses/SetDefault/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null) return NotFound();

            await ClearOtherDefaultsAsync(userId);
            address.IsDefault = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        private async Task ClearOtherDefaultsAsync(string userId)
        {
            var others = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();

            foreach (var a in others) a.IsDefault = false;
        }
    }
}