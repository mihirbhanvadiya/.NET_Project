using System.ComponentModel.DataAnnotations;

namespace OnlineJobAssignment.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty; // Read-only in view

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? Address { get; set; }

        [StringLength(500, ErrorMessage = "Skills cannot exceed 500 characters.")]
        public string? Skills { get; set; }

        public System.Collections.Generic.List<OnlineJobAssignment.Models.Review> Reviews { get; set; } = new System.Collections.Generic.List<OnlineJobAssignment.Models.Review>();

        public double AverageRating { get; set; }
    }
}
