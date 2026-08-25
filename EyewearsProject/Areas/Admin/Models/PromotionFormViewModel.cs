using System.ComponentModel.DataAnnotations;
using EyewearsProject.Models;

namespace EyewearsProject.Areas.Admin.Models
{
    public class PromotionFormViewModel
    {
        public int Id { get; set; }

        [Required, Display(Name = "Coupon Code")]
        public string Code { get; set; } = "";

        public string? Description { get; set; }

        [Display(Name = "Discount Type")]
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        [Required, Range(0.01, double.MaxValue), Display(Name = "Discount Value")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Minimum Order Amount")]
        public decimal? MinOrderAmount { get; set; }

        [Display(Name = "Maximum Discount Amount")]
        public decimal? MaxDiscountAmount { get; set; }

        [Required, Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        [Required, Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(30);

        [Display(Name = "Usage Limit")]
        public int? UsageLimit { get; set; }

        public bool IsActive { get; set; } = true;
        [Display(Name = "3D Try-On Model (.glb path)")]
        public string? TryOn3DModelUrl { get; set; }
    }
}