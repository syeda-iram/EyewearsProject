using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EyewearsProject.Models;
using EyewearsProject.Extensions;
using EyewearsProject.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace EyewearsProject.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string CartSessionKey = "Cart";

        public CartController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItem> GetCart()
        {
            return HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(CartSessionKey, cart);
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST: /Cart/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int variantId, int quantity = 1)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null) return NotFound();

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ProductVariantId == variantId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductVariantId = variant.Id,
                    ProductName = product.Name,
                    Color = variant.Color,
                    ImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                               ?? product.Images.FirstOrDefault()?.ImageUrl,
                    UnitPrice = product.DiscountPrice ?? product.Price,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int variantId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductVariantId == variantId);

            if (item != null)
            {
                if (quantity <= 0)
                    cart.Remove(item);
                else
                    item.Quantity = quantity;
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // POST: /Cart/Remove
        [HttpPost]
        public IActionResult Remove(int variantId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProductVariantId == variantId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // GET: /Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });

            var user = await _userManager.GetUserAsync(User);
            var subtotal = cart.Sum(i => i.TotalPrice);

            var savedAddresses = await _context.Addresses
                .Where(a => a.UserId == user!.Id)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.SavedAddresses = savedAddresses;

            var defaultAddress = savedAddresses.FirstOrDefault(a => a.IsDefault) ?? savedAddresses.FirstOrDefault();

            var model = new CheckoutViewModel
            {
                Items = cart,
                Subtotal = subtotal,
                GrandTotal = subtotal + 200,
                FullName = defaultAddress?.FullName ?? user?.FullName ?? "",
                Email = user?.Email ?? "",
                Phone = defaultAddress?.Phone ?? "",
                ShippingAddressLine = defaultAddress?.AddressLine ?? "",
                ShippingCity = defaultAddress?.City ?? "",
                ShippingPostalCode = defaultAddress?.PostalCode ?? "",
                ShippingCountry = defaultAddress?.Country ?? "Pakistan"
            };

            return View(model);
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });

            var subtotal = cart.Sum(i => i.TotalPrice);
            model.Items = cart;
            model.Subtotal = subtotal;

            // Re-validate and recompute the discount server-side — never trust the posted DiscountAmount
            Promotion? promo = null;
            decimal discount = 0;

            if (!string.IsNullOrWhiteSpace(model.CouponCode))
            {
                promo = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == model.CouponCode.ToUpper());
                var now = DateTime.UtcNow;

                if (promo == null || !promo.IsActive || now < promo.StartDate || now > promo.EndDate ||
                    (promo.UsageLimit.HasValue && promo.UsageCount >= promo.UsageLimit.Value) ||
                    (promo.MinOrderAmount.HasValue && subtotal < promo.MinOrderAmount.Value))
                {
                    ModelState.AddModelError(nameof(model.CouponCode), "This coupon is no longer valid.");
                }
                else
                {
                    discount = promo.DiscountType == DiscountType.Percentage
                        ? subtotal * (promo.DiscountValue / 100m)
                        : promo.DiscountValue;

                    if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount.Value)
                        discount = promo.MaxDiscountAmount.Value;
                    if (discount > subtotal) discount = subtotal;
                }
            }

            model.DiscountAmount = discount;
            model.GrandTotal = subtotal - discount + model.ShippingAmount;

            if (!model.BillingSameAsShipping)
            {
                if (string.IsNullOrWhiteSpace(model.BillingAddressLine))
                    ModelState.AddModelError(nameof(model.BillingAddressLine), "Billing address is required.");
                if (string.IsNullOrWhiteSpace(model.BillingCity))
                    ModelState.AddModelError(nameof(model.BillingCity), "Billing city is required.");
                if (string.IsNullOrWhiteSpace(model.BillingPostalCode))
                    ModelState.AddModelError(nameof(model.BillingPostalCode), "Billing postal code is required.");
                if (string.IsNullOrWhiteSpace(model.BillingCountry))
                    ModelState.AddModelError(nameof(model.BillingCountry), "Billing country is required.");
            }

            if (!ModelState.IsValid)
            {
                var user2 = await _userManager.GetUserAsync(User);
                ViewBag.SavedAddresses = await _context.Addresses
                    .Where(a => a.UserId == user2!.Id)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToListAsync();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            foreach (var item in cart)
            {
                var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId);
                if (variant == null || variant.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"{item.ProductName} ({item.Color}) no longer has enough stock.");
                    ViewBag.SavedAddresses = await _context.Addresses
                        .Where(a => a.UserId == user.Id)
                        .OrderByDescending(a => a.IsDefault)
                        .ThenByDescending(a => a.CreatedAt)
                        .ToListAsync();
                    return View(model);
                }
            }

            var order = new Order
            {
                OrderNumber = "ORD-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                OrderStatus = OrderStatus.Pending,
                PaymentStatus = model.PaymentMethod == "Cash on Delivery" ? PaymentStatus.Pending : PaymentStatus.Paid,
                TotalAmount = subtotal,
                ShippingAmount = model.ShippingAmount,
                DiscountAmount = discount,
                TaxAmount = 0,
                GrandTotal = model.GrandTotal,
                PaymentMethod = model.PaymentMethod,

                RecipientName = model.FullName,
                RecipientEmail = model.Email,
                RecipientPhone = model.Phone,

                ShippingAddressLine = model.ShippingAddressLine,
                ShippingCity = model.ShippingCity,
                ShippingPostalCode = model.ShippingPostalCode,
                ShippingCountry = model.ShippingCountry,

                BillingAddressLine = model.BillingSameAsShipping ? model.ShippingAddressLine : model.BillingAddressLine!,
                BillingCity = model.BillingSameAsShipping ? model.ShippingCity : model.BillingCity!,
                BillingPostalCode = model.BillingSameAsShipping ? model.ShippingPostalCode : model.BillingPostalCode!,
                BillingCountry = model.BillingSameAsShipping ? model.ShippingCountry : model.BillingCountry!
            };

            foreach (var item in cart)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });

                var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId);
                if (variant != null) variant.StockQuantity -= item.Quantity;
            }

            _context.Orders.Add(order);

            if (promo != null)
                promo.UsageCount += 1;

            await _context.SaveChangesAsync();

            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentMethod = model.PaymentMethod,
                Amount = model.GrandTotal,
                Status = order.PaymentStatus,
                PaidAt = order.PaymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : null
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            SaveCart(new List<CartItem>());

            return RedirectToAction("Confirmation", new { id = order.Id });
        }

        // POST: /Cart/ApplyCoupon
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            var cart = GetCart();
            var subtotal = cart.Sum(i => i.TotalPrice);

            var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == code.ToUpper());

            if (promo == null)
                return Json(new { success = false, message = "Invalid coupon code." });

            if (!promo.IsActive)
                return Json(new { success = false, message = "This coupon is no longer active." });

            var now = DateTime.UtcNow;
            if (now < promo.StartDate || now > promo.EndDate)
                return Json(new { success = false, message = "This coupon has expired or isn't active yet." });

            if (promo.UsageLimit.HasValue && promo.UsageCount >= promo.UsageLimit.Value)
                return Json(new { success = false, message = "This coupon has reached its usage limit." });

            if (promo.MinOrderAmount.HasValue && subtotal < promo.MinOrderAmount.Value)
                return Json(new { success = false, message = $"Minimum order of {promo.MinOrderAmount.Value.ToPkr()} required for this coupon." });

            decimal discount = promo.DiscountType == DiscountType.Percentage
                ? subtotal * (promo.DiscountValue / 100m)
                : promo.DiscountValue;

            if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount.Value)
                discount = promo.MaxDiscountAmount.Value;

            if (discount > subtotal) discount = subtotal;

            return Json(new
            {
                success = true,
                discountAmount = discount,
                discountText = discount.ToPkr(),
                message = $"Coupon applied: {promo.Code}"
            });
        }

        // GET: /Cart/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}