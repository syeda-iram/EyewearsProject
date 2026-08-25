using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EyewearsProject.Models
{
    public class Prescription
    {
        public int Id { get; set; }

        public string UserId { get; set; } = "";

        [ValidateNever]
        public ApplicationUser User { get; set; } = null!;

        public string PatientName { get; set; } = "";
        public string? PrescribedBy { get; set; }   // doctor / clinic name, optional
        public DateTime? IssuedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        // Right eye (OD)
        public decimal? RightSphere { get; set; }
        public decimal? RightCylinder { get; set; }
        public int? RightAxis { get; set; }
        public decimal? RightAdd { get; set; }
        public decimal? RightPd { get; set; }

        // Left eye (OS)
        public decimal? LeftSphere { get; set; }
        public decimal? LeftCylinder { get; set; }
        public int? LeftAxis { get; set; }
        public decimal? LeftAdd { get; set; }
        public decimal? LeftPd { get; set; }

        public string? Notes { get; set; }
        public string? UploadedFileUrl { get; set; } // photo or PDF of the prescription, if the customer chose to upload instead of typing values

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}