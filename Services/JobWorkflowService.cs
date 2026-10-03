using Microsoft.EntityFrameworkCore;
using OnlineJobAssignment.Data;
using OnlineJobAssignment.Models;
using OnlineJobAssignment.Models.Enums;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Services
{
    public class JobWorkflowService : IJobWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public JobWorkflowService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<(bool Success, string Message)> StartJobAsync(int jobId, string userId)
        {
            var job = await _context.Jobs
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                return (false, "Job not found.");

            // Find the accepted application to verify the seeker
            var assignedApplication = job.Applications.FirstOrDefault(a => a.Status == ApplicationStatus.Accepted);
            
            if (assignedApplication == null || assignedApplication.JobSeekerId != userId)
                return (false, "Unauthorized. Only the assigned JobSeeker can start this job.");

            if (job.Status != JobStatus.Assigned)
                return (false, "Only assigned jobs can be started.");

            job.Status = JobStatus.InProgress;
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                job.JobProviderId,
                "Job Started",
                $"The job '{job.Title}' has been started by the assigned JobSeeker."
            );

            return (true, "Job started successfully.");
        }

        public async Task<(bool Success, string Message)> CompleteJobAsync(int jobId, string userId)
        {
            var job = await _context.Jobs
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                return (false, "Job not found.");

            var assignedApplication = job.Applications.FirstOrDefault(a => a.Status == ApplicationStatus.Accepted);
            
            bool isAssignedSeeker = assignedApplication != null && assignedApplication.JobSeekerId == userId;
            bool isJobProvider = job.JobProviderId == userId;

            if (!isAssignedSeeker && !isJobProvider)
                return (false, "Unauthorized. Only the assigned JobSeeker or the Job Provider can complete this job.");

            if (job.Status != JobStatus.InProgress)
                return (false, "Only in-progress jobs can be marked as completed.");

            job.Status = JobStatus.Completed;
            await _context.SaveChangesAsync();

            string notifierRole = isAssignedSeeker ? "JobSeeker" : "Job Provider";
            string? notifyUserId = isAssignedSeeker ? job.JobProviderId : assignedApplication?.JobSeekerId;

            if (!string.IsNullOrEmpty(notifyUserId))
            {
                await _notificationService.CreateNotificationAsync(
                    notifyUserId,
                    "Job Completed",
                    $"The job '{job.Title}' has been marked as completed by the {notifierRole}."
                );
            }

            if (assignedApplication != null)
            {
                bool paymentExists = await _context.Payments.AnyAsync(p => p.JobId == job.Id);
                if (!paymentExists)
                {
                    var payment = new Payment
                    {
                        JobId = job.Id,
                        JobProviderId = job.JobProviderId,
                        JobSeekerId = assignedApplication.JobSeekerId,
                        Amount = job.PaymentAmount,
                        Status = PaymentStatus.Pending,
                        CreatedAt = System.DateTime.UtcNow
                    };
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                }
            }

            return (true, "Job marked as completed successfully.");
        }

        public async Task<(bool Success, string Message)> CancelJobAsync(int jobId, string userId)
        {
            var job = await _context.Jobs
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                return (false, "Job not found.");

            if (job.JobProviderId != userId)
                return (false, "Unauthorized. Only the Job Provider can cancel this job.");

            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Cancelled)
                return (false, "Cannot cancel a job that is already completed or cancelled.");

            job.Status = JobStatus.Cancelled;
            await _context.SaveChangesAsync();

            var assignedApplication = job.Applications.FirstOrDefault(a => a.Status == ApplicationStatus.Accepted);
            if (assignedApplication != null)
            {
                await _notificationService.CreateNotificationAsync(
                    assignedApplication.JobSeekerId,
                    "Job Cancelled",
                    $"The job '{job.Title}' you were assigned to has been cancelled by the Job Provider."
                );
            }

            return (true, "Job cancelled successfully.");
        }
    }
}
