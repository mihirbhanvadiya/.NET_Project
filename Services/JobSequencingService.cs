using System;
using System.Collections.Generic;
using System.Linq;
using JobSequenceApp.Models;
using JobSequenceApp.Models.ViewModels;

namespace JobSequenceApp.Services
{
    public class JobSequencingService : IJobSequencingService
    {
        public JobSequenceResultViewModel CalculateSequence(List<Job> jobs)
        {
            if (jobs == null || jobs.Count == 0)
            {
                return new JobSequenceResultViewModel
                {
                    AvailableJobs = new List<Job>(),
                    ScheduledJobs = new Job[0],
                    TotalProfit = 0,
                    MaxDeadline = 0
                };
            }

            // Sort jobs by descending profit
            var sortedJobs = jobs.OrderByDescending(j => j.Profit).ToList();

            // Find maximum deadline
            int maxDeadline = sortedJobs.Max(j => j.Deadline);
            
            // Array to keep track of free time slots, initialized to null
            Job[] slots = new Job[maxDeadline];

            decimal totalProfit = 0;

            foreach (var job in sortedJobs)
            {
                // Find a free slot for this job (starting from its deadline down to 1)
                // Since slots array is 0-indexed, slot for deadline d is at index d-1
                int startSlot = Math.Min(maxDeadline, job.Deadline) - 1;

                for (int j = startSlot; j >= 0; j--)
                {
                    if (slots[j] == null)
                    {
                        slots[j] = job;
                        totalProfit += job.Profit;
                        break; // Move to the next job
                    }
                }
            }

            return new JobSequenceResultViewModel
            {
                AvailableJobs = jobs,
                ScheduledJobs = slots,
                TotalProfit = totalProfit,
                MaxDeadline = maxDeadline
            };
        }
    }
}
