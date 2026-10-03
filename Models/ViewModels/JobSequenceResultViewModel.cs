using System.Collections.Generic;
using JobSequenceApp.Models;

namespace JobSequenceApp.Models.ViewModels
{
    public class JobSequenceResultViewModel
    {
        public List<Job> AvailableJobs { get; set; } = new List<Job>();
        public Job[] ScheduledJobs { get; set; }
        public decimal TotalProfit { get; set; }
        public int MaxDeadline { get; set; }
    }
}
