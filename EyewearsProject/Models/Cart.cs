namespace EyewearsProject.Models
{
    // One persistent cart per logged-in customer, stored in the DB so it
    // survives logout/login and never leaks between users on a shared browser.
    // Guests (not logged in) still get a lightweight session-based cart —
    // see CartService — which is merged into this DB cart on login.
    public class Cart
    {
        public int Id { get; set; }

        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<CartLine> Lines { get; set; } = new();
    }

    public class CartLine
    {
        public int Id { get; set; }

        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        // Matches CartItem.LineId so the same DTO/UI code works for both
        // the DB-backed cart and the guest session cart.
        public string LineId { get; set; } = Guid.NewGuid().ToString();

        public int ProductId { get; set; }
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string Color { get; set; } = "";
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public string? LensType { get; set; }
        public string? Coating { get; set; }
        public int? PrescriptionId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}