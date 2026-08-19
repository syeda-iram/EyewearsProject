namespace EyewearsProject.Areas.Admin.Models
{
    public class ReviewListItemViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}