using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Controllers
{
    [Authorize]
    public class PrescriptionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrescriptionsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Prescriptions
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var prescriptions = await _context.Prescriptions
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(prescriptions);
        }

        // GET: /Prescriptions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (prescription == null) return NotFound();

            return View(prescription);
        }

        // GET: /Prescriptions/Create
        public IActionResult Create()
        {
            return View(new Prescription());
        }

        // POST: /Prescriptions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prescription prescription)
        {
            if (!ModelState.IsValid) return View(prescription);

            var userId = _userManager.GetUserId(User)!;
            prescription.UserId = userId;
            prescription.CreatedAt = DateTime.UtcNow;

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: /Prescriptions/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (prescription == null) return NotFound();

            return View(prescription);
        }

        // POST: /Prescriptions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Prescription updated)
        {
            var userId = _userManager.GetUserId(User)!;
            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (prescription == null) return NotFound();
            if (!ModelState.IsValid) return View(updated);

            prescription.PatientName = updated.PatientName;
            prescription.PrescribedBy = updated.PrescribedBy;
            prescription.IssuedDate = updated.IssuedDate;
            prescription.ExpiryDate = updated.ExpiryDate;

            prescription.RightSphere = updated.RightSphere;
            prescription.RightCylinder = updated.RightCylinder;
            prescription.RightAxis = updated.RightAxis;
            prescription.RightAdd = updated.RightAdd;
            prescription.RightPd = updated.RightPd;

            prescription.LeftSphere = updated.LeftSphere;
            prescription.LeftCylinder = updated.LeftCylinder;
            prescription.LeftAxis = updated.LeftAxis;
            prescription.LeftAdd = updated.LeftAdd;
            prescription.LeftPd = updated.LeftPd;

            prescription.Notes = updated.Notes;
            prescription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // POST: /Prescriptions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (prescription == null) return NotFound();

            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}