using EyewearsProject.Extensions;
using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Controllers
{
    public class LensController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IInventoryService _inventoryService;
        private readonly ICartService _cartService;

        public LensController(AppDbContext context, UserManager<ApplicationUser> userManager, IInventoryService inventoryService, ICartService cartService)
        {
            _context = context;
            _userManager = userManager;
            _inventoryService = inventoryService;
            _cartService = cartService;
        }

        public async Task<IActionResult> Customize(int productId, int variantId)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null)
            {
                TempData["Error"] = "This product isn't available for purchase yet — no options have been set up.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            ViewBag.Product = product;
            ViewBag.Variant = variant;
            ViewBag.LensTypes = LensOptions.LensTypes;
            ViewBag.Coatings = LensOptions.Coatings;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.SavedPrescriptions = await _context.Prescriptions
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                ViewBag.SavedPrescriptions = new List<Prescription>();
            }

            return View();
        }

        // POST: /Lens/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(
            int productId, int variantId, string lensType, string coating, int quantity = 1,
            int? prescriptionId = null,
            string? newPatientName = null,
            decimal? rSph = null, decimal? rCyl = null, int? rAxis = null, decimal? rAdd = null, decimal? rPd = null,
            decimal? lSph = null, decimal? lCyl = null, int? lAxis = null, decimal? lAdd = null, decimal? lPd = null,
            string? uploadedFileUrl = null)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null) return NotFound();

            var available = await _inventoryService.GetAvailableQuantityAsync(variantId);
            if (available < quantity)
            {
                TempData["Error"] = available == 0
                    ? $"Sorry, {product.Name} ({variant.Color}) is currently out of stock."
                    : $"Sorry, only {available} left in stock for {product.Name} ({variant.Color}).";
                return RedirectToAction("Customize", new { productId, variantId });
            }

            decimal lensPrice = LensOptions.LensTypes.TryGetValue(lensType, out var lp) ? lp : 0;
            decimal coatingPrice = LensOptions.Coatings.TryGetValue(coating, out var cp) ? cp : 0;
            decimal basePrice = product.DiscountPrice ?? product.Price;

            // If the customer entered a prescription manually or uploaded a file
            // right here in the lens flow, save it as a real Prescription now
            // so it's also available in "My Prescriptions" going forward.
            if (prescriptionId == null && User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User)!;
                bool hasManualEntry = rSph.HasValue || rCyl.HasValue || lSph.HasValue || lCyl.HasValue;
                bool hasUpload = !string.IsNullOrWhiteSpace(uploadedFileUrl);

                if (hasManualEntry || hasUpload)
                {
                    var newRx = new Prescription
                    {
                        UserId = userId,
                        PatientName = string.IsNullOrWhiteSpace(newPatientName) ? "Prescription" : newPatientName,
                        RightSphere = rSph,
                        RightCylinder = rCyl,
                        RightAxis = rAxis,
                        RightAdd = rAdd,
                        RightPd = rPd,
                        LeftSphere = lSph,
                        LeftCylinder = lCyl,
                        LeftAxis = lAxis,
                        LeftAdd = lAdd,
                        LeftPd = lPd,
                        UploadedFileUrl = uploadedFileUrl,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Prescriptions.Add(newRx);
                    await _context.SaveChangesAsync();
                    prescriptionId = newRx.Id;
                }
            }

            await _cartService.AddAsync(new CartItem
            {
                ProductId = product.Id,
                ProductVariantId = variant.Id,
                ProductName = product.Name,
                Color = variant.Color,
                ImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
               ?? product.Images.FirstOrDefault()?.ImageUrl,
                UnitPrice = basePrice + lensPrice + coatingPrice,
                Quantity = quantity,
                LensType = lensType,
                Coating = coating,
                PrescriptionId = prescriptionId
            });

            TempData["Success"] = "Added to cart with your lens selection.";
            return RedirectToAction("Index", "Products");
        }
    }
}