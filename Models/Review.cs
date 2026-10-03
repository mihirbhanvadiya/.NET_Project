using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJobAssignment.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public virtual Job? Job { get; set; }

        [Required]
        public string ReviewerId { get; set; } = string.Empty;

        [ForeignKey(nameof(ReviewerId))]
        public virtual ApplicationUser? Reviewer { get; set; }

        [Required]
        public string RevieweeId { get; set; } = string.Empty;

        [ForeignKey(nameof(RevieweeId))]
        public virtual ApplicationUser? Reviewee { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
