using OnlineJobAssignment.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJobAssignment.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public virtual Job? Job { get; set; }

        [Required]
        public string JobProviderId { get; set; } = string.Empty;

        [ForeignKey(nameof(JobProviderId))]
        public virtual ApplicationUser? JobProvider { get; set; }

        [Required]
        public string JobSeekerId { get; set; } = string.Empty;

        [ForeignKey(nameof(JobSeekerId))]
        public virtual ApplicationUser? JobSeeker { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }
    }
}
