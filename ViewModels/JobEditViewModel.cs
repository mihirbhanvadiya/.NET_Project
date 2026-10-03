using OnlineJobAssignment.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace OnlineJobAssignment.ViewModels
{
    public class JobEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Required Skills")]
        public string? RequiredSkills { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Location { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Deadline { get; set; }

        [Required]
        [Display(Name = "Payment Amount")]
        [Range(typeof(decimal), "0.01", "1000000.00", ErrorMessage = "Payment amount must be greater than zero.")]
        public decimal PaymentAmount { get; set; }

        [Required]
        public JobStatus Status { get; set; }
    }
}
