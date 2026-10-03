using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJobAssignment.Data;
using OnlineJobAssignment.Models;
using OnlineJobAssignment.Services;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Controllers
{
    [Authorize(Roles = "JobProvider")]
    public class ProviderApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJobAssignmentService _assignmentService;

        public ProviderApplicationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IJobAssignmentService assignmentService)
        {
            _context = context;
            _userManager = userManager;
            _assignmentService = assignmentService;
        }

        // GET: ProviderApplication/Index/5 (where 5 is JobId)
        public async Task<IActionResult> Index(int jobId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.JobProviderId == user.Id);
            if (job == null) return NotFound("Job not found or unauthorized.");

            ViewData["JobTitle"] = job.Title;
            ViewData["JobId"] = job.Id;

            var applications = await _context.JobApplications
                .Include(a => a.JobSeeker)
                .Where(a => a.JobId == jobId)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            return View(applications);
        }

        // GET: ProviderApplication/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var application = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.JobSeeker)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();
            if (application.Job == null || application.Job.JobProviderId != user.Id) return Forbid();

            return View(application);
        }

        // POST: ProviderApplication/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _assignmentService.AcceptApplicationAsync(id, user.Id);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: ProviderApplication/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _assignmentService.RejectApplicationAsync(id, user.Id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
