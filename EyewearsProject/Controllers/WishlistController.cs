using EyewearsProject.Models;
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

        public WishlistController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Wishlist> GetOrCreateWishlistAsync(string userId)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Images)
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
    }
}