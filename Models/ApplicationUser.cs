using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JobSequenceApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }
    }
}
