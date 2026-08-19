namespace EyewearsProject.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string UserEmail { get; set; } = "";
        public string Action { get; set; } = "";       // e.g. "Create", "Update", "Delete", "Deactivate"
        public string EntityType { get; set; } = "";   // e.g. "Product", "User", "Category"
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}