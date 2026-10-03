using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJobAssignment.Data;
using OnlineJobAssignment.Models;
using OnlineJobAssignment.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int jobId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var job = await _context.Jobs
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null) return NotFound("Job not found");

            if (job.Status != JobStatus.Completed)
            {
                return BadRequest("You can only review a job after it has been completed.");
            }

            var assignedApplication = job.Applications.FirstOrDefault(a => a.Status == ApplicationStatus.Accepted);
            if (assignedApplication == null) return BadRequest("No accepted application found for this job.");

            bool isProvider = job.JobProviderId == user.Id;
            bool isSeeker = assignedApplication.JobSeekerId == user.Id;

            if (!isProvider && !isSeeker)
            {
                return Forbid();
            }

            string revieweeId = isProvider ? assignedApplication.JobSeekerId : job.JobProviderId;

            // Check if already reviewed
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.JobId == jobId && r.ReviewerId == user.Id && r.RevieweeId == revieweeId);

            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "You have already submitted a review for this job.";
                return RedirectToAction("Details", "Job", new { id = jobId });
            }

            var reviewee = await _userManager.FindByIdAsync(revieweeId);

            ViewBag.JobId = jobId;
            ViewBag.RevieweeName = reviewee?.FullName;
            ViewBag.JobTitle = job.Title;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int jobId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("Rating", "Rating must be between 1 and 5.");
            }

            if (!string.IsNullOrEmpty(comment) && comment.Length > 1000)
            {
                ModelState.AddModelError("Comment", "Comment cannot exceed 1000 characters.");
            }

            var job = await _context.Jobs
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null) return NotFound("Job not found");

            if (job.Status != JobStatus.Completed)
            {
                return BadRequest("You can only review a job after it has been completed.");
            }

            var assignedApplication = job.Applications.FirstOrDefault(a => a.Status == ApplicationStatus.Accepted);
            if (assignedApplication == null) return BadRequest("No accepted application found.");

            bool isProvider = job.JobProviderId == user.Id;
            bool isSeeker = assignedApplication.JobSeekerId == user.Id;

            if (!isProvider && !isSeeker)
            {
                return Forbid();
            }

            string revieweeId = isProvider ? assignedApplication.JobSeekerId : job.JobProviderId;

            // Check if already reviewed
            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.JobId == jobId && r.ReviewerId == user.Id && r.RevieweeId == revieweeId);

            if (alreadyReviewed)
            {
                return BadRequest("You have already reviewed this user for this job.");
            }

            if (ModelState.IsValid)
            {
                var review = new Review
                {
                    JobId = jobId,
                    ReviewerId = user.Id,
                    RevieweeId = revieweeId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Review submitted successfully!";
                    return RedirectToAction("Details", "Job", new { id = jobId });
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "You have already submitted a review for this job.");
                }
            }

            var reviewee = await _userManager.FindByIdAsync(revieweeId);
            ViewBag.JobId = jobId;
            ViewBag.RevieweeName = reviewee?.FullName;
            ViewBag.JobTitle = job.Title;

            return View();
        }
    }
}
