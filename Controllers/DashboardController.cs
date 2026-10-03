using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJobAssignment.Data;
using OnlineJobAssignment.Models;
using OnlineJobAssignment.Models.Enums;
using OnlineJobAssignment.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (await _userManager.IsInRoleAsync(user, "JobProvider"))
            {
                return RedirectToAction(nameof(Provider));
            }
            if (await _userManager.IsInRoleAsync(user, "JobSeeker"))
            {
                return RedirectToAction(nameof(Seeker));
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "JobProvider")]
        public async Task<IActionResult> Provider()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var myJobsQuery = _context.Jobs.Where(j => j.JobProviderId == user.Id);
            
            var totalJobs = await myJobsQuery.CountAsync();
            var openJobs = await myJobsQuery.CountAsync(j => j.Status == JobStatus.Open);
            var assignedJobs = await myJobsQuery.CountAsync(j => j.Status == JobStatus.Assigned || j.Status == JobStatus.InProgress);
            var completedJobs = await myJobsQuery.CountAsync(j => j.Status == JobStatus.Completed);

            var pendingApplications = await _context.JobApplications
                .Include(a => a.Job)
                .Where(a => a.Job != null && a.Job.JobProviderId == user.Id && a.Status == ApplicationStatus.Pending)
                .CountAsync();

            var recentApplications = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.JobSeeker)
                .Where(a => a.Job != null && a.Job.JobProviderId == user.Id)
                .OrderByDescending(a => a.ApplicationDate)
                .Take(5)
                .ToListAsync();

            var recentNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentMessages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == user.Id)
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .ToListAsync();

            var model = new ProviderDashboardViewModel
            {
                TotalJobs = totalJobs,
                OpenJobs = openJobs,
                AssignedJobs = assignedJobs,
                CompletedJobs = completedJobs,
                PendingApplications = pendingApplications,
                RecentApplications = recentApplications,
                RecentNotifications = recentNotifications,
                RecentMessages = recentMessages
            };

            return View(model);
        }

        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> Seeker()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var availableJobs = await _context.Jobs.CountAsync(j => j.Status == JobStatus.Open);

            var myAppsQuery = _context.JobApplications.Where(a => a.JobSeekerId == user.Id);
            
            var myApplications = await myAppsQuery.CountAsync();
            var pendingApplications = await myAppsQuery.CountAsync(a => a.Status == ApplicationStatus.Pending);
            var acceptedApplications = await myAppsQuery.CountAsync(a => a.Status == ApplicationStatus.Accepted);

            var assignedJobs = await myAppsQuery
                .Include(a => a.Job)
                .Where(a => a.Status == ApplicationStatus.Accepted && a.Job != null && (a.Job.Status == JobStatus.Assigned || a.Job.Status == JobStatus.InProgress))
                .CountAsync();

            var completedJobs = await myAppsQuery
                .Include(a => a.Job)
                .Where(a => a.Status == ApplicationStatus.Accepted && a.Job != null && a.Job.Status == JobStatus.Completed)
                .CountAsync();

            var recentAssignedJobs = await myAppsQuery
                .Include(a => a.Job)
                .Where(a => a.Status == ApplicationStatus.Accepted && a.Job != null)
                .OrderByDescending(a => a.Job!.CreatedAt)
                .Select(a => a.Job!)
                .Take(5)
                .ToListAsync();

            var recentNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentMessages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == user.Id)
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .ToListAsync();

            var model = new SeekerDashboardViewModel
            {
                AvailableJobs = availableJobs,
                MyApplications = myApplications,
                PendingApplications = pendingApplications,
                AcceptedApplications = acceptedApplications,
                AssignedJobs = assignedJobs,
                CompletedJobs = completedJobs,
                RecentAssignedJobs = recentAssignedJobs,
                RecentNotifications = recentNotifications,
                RecentMessages = recentMessages
            };

            return View(model);
        }
    }
}
