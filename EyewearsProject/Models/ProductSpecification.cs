namespace EyewearsProject.Models
{
    public class ProductSpecification
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string Name { get; set; } = "";   // e.g. "Frame Material", "Lens Type", "Weight"
        public string Value { get; set; } = "";  // e.g. "Acetate", "Polycarbonate", "22g"
        public int SortOrder { get; set; }
    }
}