namespace EyewearsProject.Models
{
    public class ProductAttribute
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string Name { get; set; } = "";
        public string Value { get; set; } = "";

        public int SortOrder { get; set; }
    }
}