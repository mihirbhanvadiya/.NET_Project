using OnlineJobAssignment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineJobAssignment.ViewModels
{
    public class JobSearchViewModel
    {
        public string? Keyword { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        
        [Display(Name = "Minimum Payment")]
        public decimal? MinimumPayment { get; set; }
        
        [Display(Name = "Maximum Payment")]
        public decimal? MaximumPayment { get; set; }
        
        public string? Skills { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        public IEnumerable<Job> Results { get; set; } = new List<Job>();
    }
}
