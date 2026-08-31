using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

namespace EyewearsProject.Controllers
{
    [Authorize]
    public class PrescriptionsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PrescriptionsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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

        // POST: /Prescriptions/ScanPrescription
        // Uses OCR.space's OCREngine 2, which returns per-word bounding boxes —
        // letting us reconstruct table rows/columns by real position instead of
        // guessing at raw text reading order.
        [HttpPost]
        public async Task<IActionResult> ScanPrescription(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            var apiKey = _configuration["OcrSpace:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return Json(new { success = false, message = "OCR service is not configured." });

            var httpClient = _httpClientFactory.CreateClient();

            using var formData = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            formData.Add(fileContent, "file", file.FileName);
            formData.Add(new StringContent(apiKey), "apikey");
            formData.Add(new StringContent("2"), "OCREngine");   // Engine 2 — better accuracy, gives word-level positions
            formData.Add(new StringContent("true"), "isOverlayRequired"); // returns word bounding boxes
            formData.Add(new StringContent("eng"), "language");

            HttpResponseMessage response;
            try
            {
                response = await httpClient.PostAsync("https://api.ocr.space/parse/image", formData);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Couldn't reach the OCR service: " + ex.Message });
            }

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "OCR request failed." });

            var json = await response.Content.ReadAsStringAsync();
            var words = new List<(string Text, double CenterX, double CenterY)>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("IsErroredOnProcessing", out var errEl) && errEl.GetBoolean())
                {
                    var errMsg = root.TryGetProperty("ErrorMessage", out var msgEl)
                        ? msgEl.ToString()
                        : "Unknown OCR error.";
                    return Json(new { success = false, message = "OCR error: " + errMsg });
                }

                var parsedResults = root.GetProperty("ParsedResults");
                if (parsedResults.GetArrayLength() == 0)
                    return Json(new { success = false, message = "We couldn't read any text from this image." });

                var firstResult = parsedResults[0];
                var overlay = firstResult.GetProperty("TextOverlay");
                var lines = overlay.GetProperty("Lines");

                foreach (var line in lines.EnumerateArray())
                {
                    foreach (var word in line.GetProperty("Words").EnumerateArray())
                    {
                        var text = word.GetProperty("WordText").GetString() ?? "";
                        var left = word.GetProperty("Left").GetDouble();
                        var top = word.GetProperty("Top").GetDouble();
                        var width = word.GetProperty("Width").GetDouble();
                        var height = word.GetProperty("Height").GetDouble();

                        var centerX = left + width / 2;
                        var centerY = top + height / 2;

                        if (!string.IsNullOrWhiteSpace(text))
                            words.Add((text, centerX, centerY));
                    }
                }
            }
            catch
            {
                return Json(new { success = false, message = "We couldn't read any text from this image." });
            }

            if (words.Count == 0)
                return Json(new { success = false, message = "We couldn't read any text from this image." });

            // ---- Reconstruct table rows using real Y-position ----
            var sortedByY = words.OrderBy(w => w.CenterY).ToList();
            var rows = new List<List<(string Text, double CenterX, double CenterY)>>();
            const double rowThreshold = 12;

            foreach (var w in sortedByY)
            {
                var row = rows.FirstOrDefault(r => Math.Abs(r.Average(x => x.CenterY) - w.CenterY) < rowThreshold);
                if (row != null) row.Add(w);
                else rows.Add(new List<(string, double, double)> { w });
            }

            foreach (var row in rows)
                row.Sort((a, b) => a.CenterX.CompareTo(b.CenterX));

            double FixMissingDecimal(double num)
            {
                if (Math.Abs(num) > 20 && Math.Abs(num) < 3000)
                {
                    var shifted = num / 100;
                    if (Math.Abs(shifted) <= 20) return shifted;
                }
                return num;
            }

            List<double> ExtractNumbersFromRow(List<(string Text, double CenterX, double CenterY)> row, Regex labelPattern)
            {
                var labelIndex = row.FindIndex(w => labelPattern.IsMatch(w.Text));
                if (labelIndex == -1) return new List<double>();

                var numbers = new List<double>();
                bool pendingNegative = false;

                for (int i = labelIndex + 1; i < row.Count && numbers.Count < 5; i++)
                {
                    var text = row[i].Text.Trim();

                    // OCR sometimes splits the minus sign off into its own token, separate
                    // from the number that follows it — remember it and apply it to the
                    // next number we successfully parse.
                    if (text == "-" || text == "—" || text == "–")
                    {
                        pendingNegative = true;
                        continue;
                    }

                    var cleaned = text.Replace(",", ".");
                    if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                    {
                        if (pendingNegative)
                        {
                            num = -Math.Abs(num);
                            pendingNegative = false;
                        }

                        // The decimal-drop OCR bug only ever affects SPH and CYL, which stay
                        // within roughly ±20 by nature. Axis (0-180) and PD (usually 25-75)
                        // are legitimately large numbers already — applying the same fix to
                        // them would wrongly shrink real values like 90 into 0.90.
                        var columnIndex = numbers.Count; // 0 = SPH, 1 = CYL, 2 = Axis, 3 = Add, 4 = PD
                        if (columnIndex <= 1)
                            num = FixMissingDecimal(num);

                        numbers.Add(num);
                    }
                }
                return numbers;
            }

            var odPattern = new Regex(@"^O\.?D\.?$", RegexOptions.IgnoreCase);
            var osPattern = new Regex(@"^O\.?S\.?$", RegexOptions.IgnoreCase);

            List<double>? odNums = null;
            List<double>? osNums = null;

            foreach (var row in rows)
            {
                if (odNums == null)
                {
                    var nums = ExtractNumbersFromRow(row, odPattern);
                    if (nums.Count >= 2) odNums = nums;
                }
                if (osNums == null)
                {
                    var nums = ExtractNumbersFromRow(row, osPattern);
                    if (nums.Count >= 2) osNums = nums;
                }
            }

            var result = new Dictionary<string, double>();
            if (odNums != null)
            {
                result["rSph"] = odNums[0];
                result["rCyl"] = odNums[1];
                if (odNums.Count > 2) result["rAxis"] = odNums[2];
                if (odNums.Count > 3) result["rAdd"] = odNums[3];
                if (odNums.Count > 4) result["rPd"] = odNums[4];
            }
            if (osNums != null)
            {
                result["lSph"] = osNums[0];
                result["lCyl"] = osNums[1];
                if (osNums.Count > 2) result["lAxis"] = osNums[2];
                if (osNums.Count > 3) result["lAdd"] = osNums[3];
                if (osNums.Count > 4) result["lPd"] = osNums[4];
            }

            if (result.Count == 0)
                return Json(new { success = false, message = "We couldn't clearly read prescription values from this photo. Please enter them manually." });

            return Json(new { success = true, values = result });
        }
    }
}