using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJobAssignment.Data;
using OnlineJobAssignment.Models;
using OnlineJobAssignment.Models.Enums;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Payment/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var payments = await _context.Payments
                .Include(p => p.Job)
                .Include(p => p.JobProvider)
                .Include(p => p.JobSeeker)
                .Where(p => p.JobProviderId == user.Id || p.JobSeekerId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }

        // GET: Payment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Job)
                .Include(p => p.JobProvider)
                .Include(p => p.JobSeeker)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null) return NotFound();

            // Authorization check
            if (payment.JobProviderId != user.Id && payment.JobSeekerId != user.Id)
            {
                return Forbid();
            }

            return View(payment);
        }

        // POST: Payment/Pay/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "JobProvider")]
        public async Task<IActionResult> Pay(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null) return NotFound();

            // Only JobProvider can pay
            if (payment.JobProviderId != user.Id)
            {
                return Forbid();
            }

            if (payment.Status == PaymentStatus.Paid)
            {
                TempData["Message"] = "Payment is already completed.";
                return RedirectToAction(nameof(Details), new { id = payment.Id });
            }

            // Simulate Payment Processing
            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = System.DateTime.UtcNow;
            
            _context.Update(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment simulated successfully.";
            return RedirectToAction(nameof(Details), new { id = payment.Id });
        }
    }
}
