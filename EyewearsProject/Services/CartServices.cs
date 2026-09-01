using EyewearsProject.Extensions;
using EyewearsProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Services
{
    public class CartService : ICartService
    {
        private const string CartSessionKey = "Cart";

        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(AppDbContext context, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpContext HttpContext => _httpContextAccessor.HttpContext!;

        private bool IsAuthenticated => HttpContext.User.Identity?.IsAuthenticated == true;

        private string? UserId => _userManager.GetUserId(HttpContext.User);

        // ---------- Guest (session) cart ----------

        private List<CartItem> GetSessionCart()
        {
            return HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        }

        private void SaveSessionCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(CartSessionKey, cart);
        }

        public void ClearGuestSessionCart()
        {
            HttpContext.Session.Remove(CartSessionKey);
        }

        // ---------- Logged-in (DB) cart ----------

        private async Task<Cart> GetOrCreateDbCartAsync(string userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Lines)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        private static CartItem ToCartItem(CartLine l) => new CartItem
        {
            LineId = l.LineId,
            ProductId = l.ProductId,
            ProductVariantId = l.ProductVariantId,
            ProductName = l.ProductName,
            Color = l.Color,
            ImageUrl = l.ImageUrl,
            UnitPrice = l.UnitPrice,
            Quantity = l.Quantity,
            LensType = l.LensType,
            Coating = l.Coating,
            PrescriptionId = l.PrescriptionId
        };

        private static CartLine ToCartLine(CartItem i) => new CartLine
        {
            LineId = i.LineId,
            ProductId = i.ProductId,
            ProductVariantId = i.ProductVariantId,
            ProductName = i.ProductName,
            Color = i.Color,
            ImageUrl = i.ImageUrl,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            LensType = i.LensType,
            Coating = i.Coating,
            PrescriptionId = i.PrescriptionId
        };

        // ---------- Public API ----------

        public async Task<List<CartItem>> GetCartAsync()
        {
            if (IsAuthenticated)
            {
                var cart = await GetOrCreateDbCartAsync(UserId!);
                return cart.Lines.Select(ToCartItem).ToList();
            }

            return GetSessionCart();
        }

        public async Task AddAsync(CartItem item)
        {
            // A plain (no lens customization) add can merge into an existing
            // plain line for the same variant. A lens-customized add always
            // becomes its own line — never silently merges.
            bool canMerge = item.LensType == null && item.Coating == null;

            if (IsAuthenticated)
            {
                var cart = await GetOrCreateDbCartAsync(UserId!);

                var existing = canMerge
                    ? cart.Lines.FirstOrDefault(l => l.ProductVariantId == item.ProductVariantId && l.LensType == null && l.Coating == null)
                    : null;

                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                }
                else
                {
                    var line = ToCartLine(item);
                    line.CartId = cart.Id;
                    _context.CartLines.Add(line);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                var cart = GetSessionCart();

                var existing = canMerge
                    ? cart.FirstOrDefault(c => c.ProductVariantId == item.ProductVariantId && c.LensType == null && c.Coating == null)
                    : null;

                if (existing != null)
                    existing.Quantity += item.Quantity;
                else
                    cart.Add(item);

                SaveSessionCart(cart);
            }
        }

        public async Task UpdateQuantityAsync(string lineId, int quantity)
        {
            if (IsAuthenticated)
            {
                var cart = await GetOrCreateDbCartAsync(UserId!);
                var line = cart.Lines.FirstOrDefault(l => l.LineId == lineId);
                if (line != null)
                {
                    if (quantity <= 0)
                        _context.CartLines.Remove(line);
                    else
                        line.Quantity = quantity;

                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var cart = GetSessionCart();
                var item = cart.FirstOrDefault(c => c.LineId == lineId);
                if (item != null)
                {
                    if (quantity <= 0)
                        cart.Remove(item);
                    else
                        item.Quantity = quantity;
                }
                SaveSessionCart(cart);
            }
        }

        public async Task RemoveAsync(string lineId)
        {
            if (IsAuthenticated)
            {
                var cart = await GetOrCreateDbCartAsync(UserId!);
                var line = cart.Lines.FirstOrDefault(l => l.LineId == lineId);
                if (line != null)
                {
                    _context.CartLines.Remove(line);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var cart = GetSessionCart();
                cart.RemoveAll(c => c.LineId == lineId);
                SaveSessionCart(cart);
            }
        }

        public async Task ClearAsync()
        {
            if (IsAuthenticated)
            {
                var cart = await GetOrCreateDbCartAsync(UserId!);
                if (cart.Lines.Any())
                {
                    _context.CartLines.RemoveRange(cart.Lines);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                SaveSessionCart(new List<CartItem>());
            }
        }

        public async Task MergeGuestCartIntoUserAsync()
        {
            if (!IsAuthenticated) return;

            var guestItems = GetSessionCart();
            if (!guestItems.Any())
            {
                ClearGuestSessionCart();
                return;
            }

            var cart = await GetOrCreateDbCartAsync(UserId!);

            foreach (var item in guestItems)
            {
                bool canMerge = item.LensType == null && item.Coating == null;

                var existing = canMerge
                    ? cart.Lines.FirstOrDefault(l => l.ProductVariantId == item.ProductVariantId && l.LensType == null && l.Coating == null)
                    : null;

                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                }
                else
                {
                    var line = ToCartLine(item);
                    line.CartId = cart.Id;
                    line.LineId = Guid.NewGuid().ToString(); // avoid colliding with an existing line's id
                    cart.Lines.Add(line);
                }
            }

            await _context.SaveChangesAsync();
            ClearGuestSessionCart();
        }
    }
}