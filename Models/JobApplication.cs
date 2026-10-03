using OnlineJobAssignment.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJobAssignment.Models
{
    public class JobApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public virtual Job? Job { get; set; }

        [Required]
        public string JobSeekerId { get; set; } = string.Empty;

        [ForeignKey(nameof(JobSeekerId))]
        public virtual ApplicationUser? JobSeeker { get; set; }

        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        [MaxLength(1000)]
        public string? Message { get; set; }
    }
}
