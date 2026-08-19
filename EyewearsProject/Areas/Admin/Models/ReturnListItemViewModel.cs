using EyewearsProject.Models;

namespace EyewearsProject.Areas.Admin.Models
{
    public class ReturnListItemViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public DateTime ReturnRequestDate { get; set; }
        public string Reason { get; set; } = "";
        public ReturnStatus Status { get; set; }
        public decimal RefundAmount { get; set; }
    }
}