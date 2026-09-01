using EyewearsProject.Models;
using EyewearsProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICartService _cartService;
        private readonly IInventoryService _inventoryService;

        public WishlistController(AppDbContext context, UserManager<ApplicationUser> userManager, ICartService cartService, IInventoryService inventoryService)
        {
            _context = context;
            _userManager = userManager;
            _cartService = cartService;
            _inventoryService = inventoryService;
        }

        private async Task<Wishlist> GetOrCreateWishlistAsync(string userId)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Images)
                .Include(w => w.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Variants)
                .Include(w => w.Items)
                    .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wishlist == null)
            {
                wishlist = new Wishlist { UserId = userId };
                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();
            }

            return wishlist;
        }

        // GET: /Wishlist
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var wishlist = await GetOrCreateWishlistAsync(userId);
            return View(wishlist);
        }

        // POST: /Wishlist/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int? productVariantId, string? returnUrl)
        {
            var userId = _userManager.GetUserId(User)!;
            var wishlist = await GetOrCreateWishlistAsync(userId);

            bool alreadyThere = wishlist.Items.Any(i =>
                i.ProductId == productId && i.ProductVariantId == productVariantId);

            if (!alreadyThere)
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    WishlistId = wishlist.Id,
                    ProductId = productId,
                    ProductVariantId = productVariantId
                });
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index");
        }

        // POST: /Wishlist/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int itemId)
        {
            var userId = _userManager.GetUserId(User)!;

            var item = await _context.WishlistItems
                .Include(i => i.Wishlist)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null || item.Wishlist.UserId != userId)
            {
                return NotFound();
            }

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Picks the variant to add to cart for a wishlist item: the one the
        // customer saved (if any), otherwise the first in-stock variant.
        private async Task<(ProductVariant? Variant, int Available)> ResolveCartVariantAsync(WishlistItem item)
        {
            if (item.ProductVariant != null)
            {
                var available = await _inventoryService.GetAvailableQuantityAsync(item.ProductVariant.Id);
                return (item.ProductVariant, available);
            }

            foreach (var variant in item.Product.Variants)
            {
                var available = await _inventoryService.GetAvailableQuantityAsync(variant.Id);
                if (available > 0)
                    return (variant, available);
            }

            return (item.Product.Variants.FirstOrDefault(), 0);
        }

        // POST: /Wishlist/AddToCart  (single item)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int itemId)
        {
            var userId = _userManager.GetUserId(User)!;

            var item = await _context.WishlistItems
                .Include(i => i.Wishlist)
                .Include(i => i.Product)
                    .ThenInclude(p => p.Images)
                .Include(i => i.Product)
                    .ThenInclude(p => p.Variants)
                .Include(i => i.ProductVariant)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null || item.Wishlist.UserId != userId)
                return NotFound();

            var (variant, available) = await ResolveCartVariantAsync(item);

            if (variant == null || available <= 0)
            {
                TempData["Error"] = $"{item.Product.Name} is currently out of stock and couldn't be added to your cart.";
                return RedirectToAction("Index");
            }

            await _cartService.AddAsync(new CartItem
            {
                ProductId = item.Product.Id,
                ProductVariantId = variant.Id,
                ProductName = item.Product.Name,
                Color = variant.Color,
                ImageUrl = item.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                           ?? item.Product.Images.FirstOrDefault()?.ImageUrl,
                UnitPrice = item.Product.DiscountPrice ?? item.Product.Price,
                Quantity = 1
            });

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{item.Product.Name} moved to your cart.";
            return RedirectToAction("Index");
        }

        // POST: /Wishlist/AddAllToCart  (whole wishlist)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAllToCart()
        {
            var userId = _userManager.GetUserId(User)!;
            var wishlist = await GetOrCreateWishlistAsync(userId);

            int added = 0, skipped = 0;
            var moved = new List<WishlistItem>();

            foreach (var item in wishlist.Items.ToList())
            {
                var (variant, available) = await ResolveCartVariantAsync(item);

                if (variant == null || available <= 0)
                {
                    skipped++;
                    continue;
                }

                await _cartService.AddAsync(new CartItem
                {
                    ProductId = item.Product.Id,
                    ProductVariantId = variant.Id,
                    ProductName = item.Product.Name,
                    Color = variant.Color,
                    ImageUrl = item.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                               ?? item.Product.Images.FirstOrDefault()?.ImageUrl,
                    UnitPrice = item.Product.DiscountPrice ?? item.Product.Price,
                    Quantity = 1
                });

                moved.Add(item);
                added++;
            }

            if (moved.Any())
            {
                _context.WishlistItems.RemoveRange(moved);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = skipped == 0
                ? $"Moved {added} item(s) to your cart."
                : $"Moved {added} item(s) to your cart. {skipped} item(s) were out of stock and left in your wishlist.";

            return RedirectToAction("Index");
        }
    }
}