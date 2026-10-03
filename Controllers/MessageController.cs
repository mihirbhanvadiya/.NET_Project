using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJobAssignment.Data;
using OnlineJobAssignment.Models;
using OnlineJobAssignment.Models.Enums;
using OnlineJobAssignment.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Message/Index/5 (jobId)
        public async Task<IActionResult> Index(int? jobId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!jobId.HasValue || jobId.Value <= 0)
            {
                var latestMessage = await _context.Messages
                    .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                if (latestMessage != null)
                {
                    return RedirectToAction(nameof(Index), new { jobId = latestMessage.JobId });
                }

                bool isProviderRole = await _userManager.IsInRoleAsync(user, "JobProvider");
                if (isProviderRole)
                {
                    var activeJob = await _context.Jobs.FirstOrDefaultAsync(j => j.JobProviderId == user.Id && (j.Status == JobStatus.Assigned || j.Status == JobStatus.InProgress));
                    if (activeJob != null)
                    {
                        return RedirectToAction(nameof(Index), new { jobId = activeJob.Id });
                    }
                    TempData["ErrorMessage"] = "No active conversations. Assign an applicant to a job to start messaging.";
                    return RedirectToAction("Provider", "Dashboard");
                }
                else
                {
                    var activeApp = await _context.JobApplications.Include(a => a.Job).FirstOrDefaultAsync(a => a.JobSeekerId == user.Id && a.Status == ApplicationStatus.Accepted);
                    if (activeApp != null)
                    {
                        return RedirectToAction(nameof(Index), new { jobId = activeApp.JobId });
                    }
                    TempData["ErrorMessage"] = "No active conversations. You can message the provider once your application is accepted.";
                    return RedirectToAction("Seeker", "Dashboard");
                }
            }

            var job = await _context.Jobs
                .Include(j => j.JobProvider)
                .Include(j => j.Applications).ThenInclude(a => a.JobSeeker)
                .FirstOrDefaultAsync(j => j.Id == jobId.Value);

            if (job == null) return NotFound("Job not found.");

            // Check if job is assigned, in-progress, or completed
            if (job.Status == JobStatus.Open || job.Status == JobStatus.Cancelled)
            {
                return BadRequest("Messaging is only available for active assigned jobs.");
            }

            var acceptedApp = job.Applications.FirstOrDefault(a => a.Status == ApplicationStatus.Accepted);
            if (acceptedApp == null)
            {
                return BadRequest("No assigned JobSeeker found.");
            }

            bool isProvider = job.JobProviderId == user.Id;
            bool isSeeker = acceptedApp.JobSeekerId == user.Id;

            if (!isProvider && !isSeeker)
            {
                return Forbid();
            }

            // Mark unread messages as read
            var unreadMessages = await _context.Messages
                .Where(m => m.JobId == jobId && m.ReceiverId == user.Id && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.JobId == jobId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var model = new MessageConversationViewModel
            {
                Job = job,
                Messages = messages,
                CurrentUserId = user.Id,
                OtherUserName = (isProvider ? acceptedApp.JobSeeker?.UserName : job.JobProvider?.UserName) ?? "User"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int jobId, string NewMessageContent)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(NewMessageContent) || NewMessageContent.Length > 1000)
            {
                TempData["ErrorMessage"] = "Message must be between 1 and 1000 characters.";
                return RedirectToAction(nameof(Index), new { jobId = jobId });
            }

            var job = await _context.Jobs
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null) return NotFound("Job not found.");

            if (job.Status == JobStatus.Open || job.Status == JobStatus.Cancelled)
            {
                return BadRequest("Messaging is only available for active assigned jobs.");
            }

            var acceptedApp = job.Applications.FirstOrDefault(a => a.Status == ApplicationStatus.Accepted);
            if (acceptedApp == null)
            {
                return BadRequest("No assigned JobSeeker found.");
            }

            bool isProvider = job.JobProviderId == user.Id;
            bool isSeeker = acceptedApp.JobSeekerId == user.Id;

            if (!isProvider && !isSeeker)
            {
                return Forbid();
            }

            string receiverId = isProvider ? acceptedApp.JobSeekerId : job.JobProviderId;

            var message = new Message
            {
                JobId = jobId,
                SenderId = user.Id,
                ReceiverId = receiverId,
                Content = NewMessageContent,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { jobId = jobId });
        }
    }
}
