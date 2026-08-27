namespace EyewearsProject.Models
{
    public class ProductTag
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string Name { get; set; } = "";
    }
}