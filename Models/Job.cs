using System.ComponentModel.DataAnnotations;

namespace JobSequenceApp.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Job Name/ID")]
        public string Name { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Deadline must be at least 1.")]
        public int Deadline { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Profit must be a positive value.")]
        public decimal Profit { get; set; }
    }
}
