using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJobAssignment.Data;
using OnlineJobAssignment.Models;
using OnlineJobAssignment.Models.Enums;
using OnlineJobAssignment.ViewModels;
using OnlineJobAssignment.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public ApplicationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Application/Index (My Applications)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var applications = await _context.JobApplications
                .Include(a => a.Job)
                .Where(a => a.JobSeekerId == user.Id)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            return View(applications);
        }

        // GET: Application/Apply/5
        public async Task<IActionResult> Apply(int? jobId)
        {
            if (jobId == null) return NotFound();

            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null || job.Status != JobStatus.Open)
            {
                return NotFound("Job is not available.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Cannot apply to own job (redundant but safe)
            if (job.JobProviderId == user.Id)
            {
                return BadRequest("Cannot apply to your own job.");
            }

            // Check if already applied
            var existingApp = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobId == jobId && a.JobSeekerId == user.Id);
            if (existingApp != null)
            {
                TempData["ErrorMessage"] = "You have already applied to this job.";
                return RedirectToAction("Details", "Job", new { id = jobId });
            }

            var model = new ApplyViewModel
            {
                JobId = job.Id,
                JobTitle = job.Title
            };

            return View(model);
        }

        // POST: Application/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var job = await _context.Jobs.FindAsync(model.JobId);
            if (job == null || job.Status != JobStatus.Open)
            {
                return NotFound("Job is not available.");
            }

            if (job.JobProviderId == user.Id)
            {
                return BadRequest("Cannot apply to your own job.");
            }

            var existingApp = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobId == model.JobId && a.JobSeekerId == user.Id);
            if (existingApp != null)
            {
                ModelState.AddModelError(string.Empty, "You have already applied to this job.");
                return View(model);
            }

            var application = new JobApplication
            {
                JobId = model.JobId,
                JobSeekerId = user.Id,
                Message = model.Message,
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow
            };

            try
            {
                _context.JobApplications.Add(application);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "You have already applied to this job.");
                return View(model);
            }

            await _notificationService.CreateNotificationAsync(
                job.JobProviderId,
                "New Job Application",
                $"User {user.UserName} has applied to your job '{job.Title}'."
            );

            TempData["SuccessMessage"] = "Successfully applied for the job!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Application/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == id && a.JobSeekerId == user.Id);

            if (application == null) return NotFound();

            return View(application);
        }

        // POST: Application/Withdraw/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id && a.JobSeekerId == user.Id);

            if (application == null) return NotFound();

            if (application.Status != ApplicationStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending applications can be withdrawn.";
                return RedirectToAction(nameof(Details), new { id = application.Id });
            }

            application.Status = ApplicationStatus.Withdrawn;
            _context.Update(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application withdrawn successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
