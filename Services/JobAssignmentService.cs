using Microsoft.EntityFrameworkCore;
using OnlineJobAssignment.Data;
using OnlineJobAssignment.Models.Enums;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Services
{
    public class JobAssignmentService : IJobAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public JobAssignmentService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<(bool Success, string Message)> AcceptApplicationAsync(int applicationId, string providerId)
        {
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (_context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            }

            try
            {
                var application = await _context.JobApplications
                    .Include(a => a.Job)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                    return (false, "Application not found.");

                if (application.Job == null || application.Job.JobProviderId != providerId)
                    return (false, "Unauthorized. You do not own this job.");

                if (application.Status != ApplicationStatus.Pending)
                    return (false, "Only pending applications can be accepted.");

                if (application.Job.Status != JobStatus.Open)
                    return (false, "Job is no longer open for assignments.");

                var alreadyAssigned = await _context.JobApplications
                    .AnyAsync(a => a.JobId == application.JobId && a.Status == ApplicationStatus.Accepted);
                if (alreadyAssigned)
                    return (false, "This job already has an accepted applicant.");

                // Accept the application
                application.Status = ApplicationStatus.Accepted;

                // Update the job status
                application.Job.Status = JobStatus.Assigned;

                // Reject all other pending applications for this job
                var otherPendingApps = await _context.JobApplications
                    .Where(a => a.JobId == application.JobId && a.Id != applicationId && a.Status == ApplicationStatus.Pending)
                    .ToListAsync();

                foreach (var otherApp in otherPendingApps)
                {
                    otherApp.Status = ApplicationStatus.Rejected;
                }

                await _context.SaveChangesAsync();

                // Send notifications
                await _notificationService.CreateNotificationAsync(
                    application.JobSeekerId,
                    "Application Accepted",
                    $"Your application for the job '{application.Job.Title}' was accepted! The job is now assigned to you."
                );

                foreach (var otherApp in otherPendingApps)
                {
                    await _notificationService.CreateNotificationAsync(
                        otherApp.JobSeekerId,
                        "Application Rejected",
                        $"Your application for the job '{application.Job.Title}' was rejected because another applicant was assigned."
                    );
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return (true, "Application accepted successfully. The job is now assigned.");
            }
            catch (System.Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                return (false, $"An error occurred: {ex.Message}");
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        public async Task<(bool Success, string Message)> RejectApplicationAsync(int applicationId, string providerId)
        {
            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
                return (false, "Application not found.");

            if (application.Job == null || application.Job.JobProviderId != providerId)
                return (false, "Unauthorized. You do not own this job.");

            if (application.Status != ApplicationStatus.Pending)
                return (false, "Only pending applications can be rejected.");

            application.Status = ApplicationStatus.Rejected;
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                application.JobSeekerId,
                "Application Rejected",
                $"Your application for the job '{application.Job.Title}' was rejected."
            );

            return (true, "Application rejected successfully.");
        }
    }
}
