namespace EyewearsProject.Models
{
    // One wishlist per customer.
    public class Wishlist
    {
        public int Id { get; set; }

        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<WishlistItem> Items { get; set; } = new();
    }

    public class WishlistItem
    {
        public int Id { get; set; }

        public int WishlistId { get; set; }
        public Wishlist Wishlist { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}