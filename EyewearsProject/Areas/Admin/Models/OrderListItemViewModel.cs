using EyewearsProject.Models;

namespace EyewearsProject.Areas.Admin.Models
{
    public class OrderListItemViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public decimal GrandTotal { get; set; }
        public int ItemCount { get; set; }
    }
}