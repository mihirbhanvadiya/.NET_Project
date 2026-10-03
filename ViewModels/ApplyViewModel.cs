using System.ComponentModel.DataAnnotations;

namespace OnlineJobAssignment.ViewModels
{
    public class ApplyViewModel
    {
        [Required]
        public int JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Display(Name = "Cover Letter / Message")]
        public string? Message { get; set; }
    }
}
