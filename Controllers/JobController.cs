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
    [Authorize]
    public class JobController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJobWorkflowService _workflowService;

        public JobController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IJobWorkflowService workflowService)
        {
            _context = context;
            _userManager = userManager;
            _workflowService = workflowService;
        }

        // GET: Job (Provider Dashboard)
        [Authorize(Roles = "JobProvider")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var jobs = await _context.Jobs
                .Where(j => j.JobProviderId == user.Id)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return View(jobs);
        }

        // GET: Job/Search (Seeker Dashboard)
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> Search(JobSearchViewModel model)
        {
            var query = _context.Jobs.Include(j => j.JobProvider).Where(j => j.Status == JobStatus.Open).AsQueryable();

            if (!string.IsNullOrWhiteSpace(model.Keyword))
            {
                var kw = model.Keyword.ToLower();
                query = query.Where(j => j.Title.ToLower().Contains(kw) 
                                      || j.Description.ToLower().Contains(kw) 
                                      || (j.RequiredSkills != null && j.RequiredSkills.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(model.Category))
            {
                query = query.Where(j => j.Category.Contains(model.Category));
            }

            if (!string.IsNullOrWhiteSpace(model.Location))
            {
                query = query.Where(j => j.Location != null && j.Location.Contains(model.Location));
            }

            if (!string.IsNullOrWhiteSpace(model.Skills))
            {
                query = query.Where(j => j.RequiredSkills != null && j.RequiredSkills.Contains(model.Skills));
            }

            if (model.MinimumPayment.HasValue)
            {
                query = query.Where(j => j.PaymentAmount >= model.MinimumPayment.Value);
            }

            if (model.MaximumPayment.HasValue)
            {
                query = query.Where(j => j.PaymentAmount <= model.MaximumPayment.Value);
            }

            if (model.Deadline.HasValue)
            {
                query = query.Where(j => j.Deadline.Date <= model.Deadline.Value.Date);
            }

            model.Results = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

            return View(model);
        }

        // GET: Job/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var job = await _context.Jobs.Include(j => j.JobProvider)
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (job == null) return NotFound();

            bool isProvider = await _userManager.IsInRoleAsync(user, "JobProvider");
            bool isSeeker = await _userManager.IsInRoleAsync(user, "JobSeeker");

            if (isProvider)
            {
                if (job.JobProviderId != user.Id) return Forbid();
            }
            else if (isSeeker)
            {
                bool isAssignedSeeker = job.Applications.Any(a => a.JobSeekerId == user.Id && a.Status == ApplicationStatus.Accepted);
                ViewData["IsAssignedSeeker"] = isAssignedSeeker;

                if (job.Status != JobStatus.Open && !isAssignedSeeker) 
                {
                    if (!job.Applications.Any(a => a.JobSeekerId == user.Id))
                    {
                        return Forbid();
                    }
                }
            }
            else
            {
                return Forbid();
            }

            return View(job);
        }

        // GET: Job/Create
        [Authorize(Roles = "JobProvider")]
        public IActionResult Create()
        {
            return View(new JobCreateViewModel());
        }

        // POST: Job/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JobProvider")]
        public async Task<IActionResult> Create(JobCreateViewModel model)
        {
            if (model.Deadline.Date <= DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("Deadline", "Job deadline must be in the future.");
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var job = new Job
                {
                    Title = model.Title,
                    Description = model.Description,
                    RequiredSkills = model.RequiredSkills,
                    Category = model.Category,
                    Location = model.Location,
                    Deadline = model.Deadline,
                    PaymentAmount = model.PaymentAmount,
                    JobProviderId = user.Id,
                    Status = JobStatus.Open,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Job/Edit/5
        [Authorize(Roles = "JobProvider")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.JobProviderId == user.Id);
            if (job == null) return NotFound();

            if (job.Status != JobStatus.Open)
            {
                TempData["ErrorMessage"] = "Only open jobs can be edited.";
                return RedirectToAction(nameof(Index));
            }

            var model = new JobEditViewModel
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                RequiredSkills = job.RequiredSkills,
                Category = job.Category,
                Location = job.Location,
                Deadline = job.Deadline,
                PaymentAmount = job.PaymentAmount,
                Status = job.Status
            };

            return View(model);
        }

        // POST: Job/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JobProvider")]
        public async Task<IActionResult> Edit(int id, JobEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (model.Deadline.Date <= DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("Deadline", "Job deadline must be in the future.");
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.JobProviderId == user.Id);
                if (job == null) return NotFound();

                if (job.Status != JobStatus.Open)
                {
                    TempData["ErrorMessage"] = "Only open jobs can be edited.";
                    return RedirectToAction(nameof(Index));
                }

                job.Title = model.Title;
                job.Description = model.Description;
                job.RequiredSkills = model.RequiredSkills;
                job.Category = model.Category;
                job.Location = model.Location;
                job.Deadline = model.Deadline;
                job.PaymentAmount = model.PaymentAmount;

                try
                {
                    _context.Update(job);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(job.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Job/Delete/5
        [Authorize(Roles = "JobProvider")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var job = await _context.Jobs
                .FirstOrDefaultAsync(m => m.Id == id && m.JobProviderId == user.Id);
            if (job == null) return NotFound();

            if (job.Status != JobStatus.Open && job.Status != JobStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cannot delete a job that is assigned, in-progress, or completed.";
                return RedirectToAction(nameof(Index));
            }

            return View(job);
        }

        // POST: Job/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JobProvider")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.JobProviderId == user.Id);
            if (job != null)
            {
                if (job.Status != JobStatus.Open && job.Status != JobStatus.Cancelled)
                {
                    TempData["ErrorMessage"] = "Cannot delete a job that is assigned, in-progress, or completed.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool JobExists(int id)
        {
            return _context.Jobs.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> StartJob(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _workflowService.StartJobAsync(id, user.Id);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteJob(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _workflowService.CompleteJobAsync(id, user.Id);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JobProvider")]
        public async Task<IActionResult> CancelJob(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _workflowService.CancelJobAsync(id, user.Id);
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
