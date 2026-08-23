using EyewearsProject.Extensions;
using EyewearsProject.Models;
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
        private const string CartSessionKey = "Cart";

        public LensController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Lens/Customize?productId=1&variantId=2
        public async Task<IActionResult> Customize(int productId, int variantId)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null) return NotFound();

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
        public async Task<IActionResult> AddToCart(int productId, int variantId, string lensType, string coating, int? prescriptionId, int quantity = 1)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null) return NotFound();

            // Re-check the prices server-side — never trust the posted lens/coating
            // strings for price, only for which option was chosen.
            decimal lensPrice = LensOptions.LensTypes.TryGetValue(lensType, out var lp) ? lp : 0;
            decimal coatingPrice = LensOptions.Coatings.TryGetValue(coating, out var cp) ? cp : 0;
            decimal basePrice = product.DiscountPrice ?? product.Price;

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();

            cart.Add(new CartItem
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

            HttpContext.Session.SetObject(CartSessionKey, cart);

            TempData["Success"] = "Added to cart with your lens selection.";
            return RedirectToAction("Index", "Products");
        }
    }
}