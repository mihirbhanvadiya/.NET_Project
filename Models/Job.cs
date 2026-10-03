using OnlineJobAssignment.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJobAssignment.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RequiredSkills { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Location { get; set; }

        [Required]
        public DateTime Deadline { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaymentAmount { get; set; }

        [Required]
        public JobStatus Status { get; set; } = JobStatus.Open;

        [Required]
        public string JobProviderId { get; set; } = string.Empty;

        [ForeignKey(nameof(JobProviderId))]
        public virtual ApplicationUser? JobProvider { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    }
}
