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
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrescriptionsController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
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
        private static readonly string[] AllowedPrescriptionExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
        private const long MaxPrescriptionFileBytes = 8 * 1024 * 1024; // 8 MB

        // POST: /Prescriptions/UploadFile
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            if (file.Length > MaxPrescriptionFileBytes)
                return Json(new { success = false, message = "File too large. Max size is 8 MB." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedPrescriptionExtensions.Contains(ext))
                return Json(new { success = false, message = "Only JPG, PNG, or PDF files are allowed." });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "prescriptions");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/prescriptions/{fileName}";
            return Json(new { success = true, url = publicUrl });
        }
    }
}