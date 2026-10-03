using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using OnlineJobAssignment.Controllers;
using OnlineJobAssignment.Data;
using OnlineJobAssignment.Models;
using OnlineJobAssignment.Models.Enums;
using OnlineJobAssignment.Services;
using OnlineJobAssignment.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace OnlineJobAssignment.Tests
{
    public class FunctionalWorkflowsAndSecurityTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private Mock<UserManager<ApplicationUser>> CreateMockUserManager(List<ApplicationUser> users, ApplicationDbContext context)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser user, string password) =>
                {
                    if (string.IsNullOrEmpty(user.Id)) user.Id = Guid.NewGuid().ToString();
                    users.Add(user);
                    if (!context.Users.Any(u => u.Id == user.Id))
                    {
                        context.Users.Add(user);
                        context.SaveChanges();
                    }
                    return IdentityResult.Success;
                });

            userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => users.FirstOrDefault(u => u.Id == id));

            userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string email) => users.FirstOrDefault(u => u.Email == email));

            userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((ClaimsPrincipal principal) =>
                {
                    var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    return users.FirstOrDefault(u => u.Id == id);
                });

            userManager.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser u, string role) =>
                {
                    if (u.Email != null && u.Email.Contains("provider", StringComparison.OrdinalIgnoreCase))
                        return role == "JobProvider";
                    if (u.Email != null && u.Email.Contains("seeker", StringComparison.OrdinalIgnoreCase))
                        return role == "JobSeeker";
                    return false;
                });

            return userManager;
        }

        private Mock<SignInManager<ApplicationUser>> CreateMockSignInManager(Mock<UserManager<ApplicationUser>> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var signInManager = new Mock<SignInManager<ApplicationUser>>(
                userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

            signInManager.Setup(m => m.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync((string email, string password, bool remember, bool lockout) =>
                {
                    if (password == "ValidPass123!")
                        return Microsoft.AspNetCore.Identity.SignInResult.Success;
                    return Microsoft.AspNetCore.Identity.SignInResult.Failed;
                });

            return signInManager;
        }

        private ControllerContext CreateControllerContext(ApplicationUser user, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? "user"),
                new Claim(ClaimTypes.Email, user.Email ?? "user@example.com"),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            return new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Workflows_1_to_6_CompleteEndToEndSuccess()
        {
            // Setup DB and Services
            var context = CreateInMemoryDbContext();
            var users = new List<ApplicationUser>();
            var userManager = CreateMockUserManager(users);
            var signInManager = CreateMockSignInManager(userManager);

            var notificationService = new NotificationService(context);
            var workflowService = new JobWorkflowService(context, notificationService);
            var assignmentService = new JobAssignmentService(context, notificationService);

            // ==========================================
            // WORKFLOW 1: Register Job Provider, Login, Create Job, Edit Job, View Job
            // ==========================================
            var accountController = new AccountController(userManager.Object, signInManager.Object);
            
            // 1.1 Register Provider
            var providerReg = new RegisterViewModel
            {
                FullName = "Alice Provider",
                Email = "provider@test.com",
                Password = "ValidPass123!",
                ConfirmPassword = "ValidPass123!",
                UserRole = "JobProvider"
            };
            var regResult = await accountController.Register(providerReg);
            Assert.IsType<RedirectToActionResult>(regResult);
            var providerUser = users.First(u => u.Email == "provider@test.com");

            // 1.2 Login Provider
            var loginResult = await accountController.Login(new LoginViewModel
            {
                Email = "provider@test.com",
                Password = "ValidPass123!"
            });
            Assert.IsType<RedirectToActionResult>(loginResult);

            // 1.3 Create Job
            var jobController = new JobController(context, userManager.Object, workflowService);
            jobController.ControllerContext = CreateControllerContext(providerUser, "JobProvider");
            jobController.TempData = new TempDataDictionary(jobController.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var createModel = new JobCreateViewModel
            {
                Title = "ASP.NET Core Web App",
                Description = "Develop an online portal using ASP.NET Core MVC.",
                Category = "Software Development",
                RequiredSkills = "C#, ASP.NET, SQL",
                Location = "Remote",
                Deadline = DateTime.UtcNow.AddDays(14),
                PaymentAmount = 500.00m
            };
            var createJobResult = await jobController.Create(createModel);
            Assert.IsType<RedirectToActionResult>(createJobResult);

            var createdJob = await context.Jobs.FirstOrDefaultAsync(j => j.JobProviderId == providerUser.Id);
            Assert.NotNull(createdJob);
            Assert.Equal("ASP.NET Core Web App", createdJob.Title);
            Assert.Equal(JobStatus.Open, createdJob.Status);

            // 1.4 Edit Job (Allowed when Open)
            var editModel = new JobEditViewModel
            {
                Id = createdJob.Id,
                Title = "ASP.NET Core 10 Web App",
                Description = "Updated description with latest specifications.",
                Category = "Software Development",
                RequiredSkills = "C#, ASP.NET Core, EF Core",
                Location = "Remote",
                Deadline = DateTime.UtcNow.AddDays(20),
                PaymentAmount = 600.00m
            };
            var editResult = await jobController.Edit(createdJob.Id, editModel);
            Assert.IsType<RedirectToActionResult>(editResult);

            var editedJob = await context.Jobs.FindAsync(createdJob.Id);
            Assert.Equal("ASP.NET Core 10 Web App", editedJob!.Title);
            Assert.Equal(600.00m, editedJob.PaymentAmount);

            // 1.5 View Job Details as Provider
            var viewResult = await jobController.Details(editedJob.Id);
            var viewViewResult = Assert.IsType<ViewResult>(viewResult);
            Assert.Equal(editedJob.Id, ((Job)viewViewResult.Model!).Id);

            // ==========================================
            // WORKFLOW 2: Register Job Seeker, Login, Search Jobs, Filter Jobs, View Details, Apply
            // ==========================================
            // 2.1 Register Seeker
            var seekerReg = new RegisterViewModel
            {
                FullName = "Bob Seeker",
                Email = "seeker@test.com",
                Password = "ValidPass123!",
                ConfirmPassword = "ValidPass123!",
                UserRole = "JobSeeker"
            };
            await accountController.Register(seekerReg);
            var seekerUser = users.First(u => u.Email == "seeker@test.com");

            // 2.2 Search & Filter Jobs as Seeker
            var seekerJobController = new JobController(context, userManager.Object, workflowService);
            seekerJobController.ControllerContext = CreateControllerContext(seekerUser, "JobSeeker");
            seekerJobController.TempData = new TempDataDictionary(seekerJobController.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var searchModel = new JobSearchViewModel
            {
                Keyword = "Core",
                Category = "Software",
                MinimumPayment = 500
            };
            var searchResult = await seekerJobController.Search(searchModel);
            var searchView = Assert.IsType<ViewResult>(searchResult);
            var searchResults = ((JobSearchViewModel)searchView.Model!).Results.ToList();
            Assert.Single(searchResults);
            Assert.Equal(editedJob.Id, searchResults[0].Id);

            // 2.3 View Details as Seeker
            var seekerDetailsResult = await seekerJobController.Details(editedJob.Id);
            Assert.IsType<ViewResult>(seekerDetailsResult);

            // 2.4 Apply for Job
            var appController = new ApplicationController(context, userManager.Object, notificationService);
            appController.ControllerContext = CreateControllerContext(seekerUser, "JobSeeker");
            appController.TempData = new TempDataDictionary(appController.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var applyModel = new ApplyViewModel
            {
                JobId = editedJob.Id,
                JobTitle = editedJob.Title,
                Message = "I am an expert C# developer and would love to work on this."
            };
            var applyResult = await appController.Apply(applyModel);
            Assert.IsType<RedirectToActionResult>(applyResult);

            var application = await context.JobApplications.FirstOrDefaultAsync(a => a.JobId == editedJob.Id && a.JobSeekerId == seekerUser.Id);
            Assert.NotNull(application);
            Assert.Equal(ApplicationStatus.Pending, application.Status);

            // ==========================================
            // WORKFLOW 3: Provider Receives Notification, Views Application, Accepts Application
            // ==========================================
            // 3.1 Provider Notification Check
            var providerNotifications = await notificationService.GetUserNotificationsAsync(providerUser.Id);
            Assert.Single(providerNotifications);
            Assert.Contains("New Job Application", providerNotifications[0].Title);

            // 3.2 Provider Views Applications
            var providerAppController = new ProviderApplicationController(context, userManager.Object, assignmentService);
            providerAppController.ControllerContext = CreateControllerContext(providerUser, "JobProvider");
            providerAppController.TempData = new TempDataDictionary(providerAppController.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var appListResult = await providerAppController.Index(editedJob.Id);
            var appListView = Assert.IsType<ViewResult>(appListResult);
            var apps = (List<JobApplication>)appListView.Model!;
            Assert.Single(apps);

            // 3.3 Provider Accepts Application
            var acceptResult = await providerAppController.Accept(application.Id);
            Assert.IsType<RedirectToActionResult>(acceptResult);

            var updatedJob = await context.Jobs.FindAsync(editedJob.Id);
            var updatedApp = await context.JobApplications.FindAsync(application.Id);
            Assert.Equal(JobStatus.Assigned, updatedJob!.Status);
            Assert.Equal(ApplicationStatus.Accepted, updatedApp!.Status);

            // ==========================================
            // WORKFLOW 4: Seeker Receives Acceptance Notification, Views Assigned Job, Starts Job, Completes Job
            // ==========================================
            // 4.1 Seeker receives acceptance notification
            var seekerNotifications = await notificationService.GetUserNotificationsAsync(seekerUser.Id);
            Assert.Contains(seekerNotifications, n => n.Title == "Application Accepted");

            // 4.2 Seeker Starts Job
            var startResult = await seekerJobController.StartJob(editedJob.Id);
            Assert.IsType<RedirectToActionResult>(startResult);
            var inProgressJob = await context.Jobs.FindAsync(editedJob.Id);
            Assert.Equal(JobStatus.InProgress, inProgressJob!.Status);

            // 4.3 Seeker Completes Job
            var completeResult = await seekerJobController.CompleteJob(editedJob.Id);
            Assert.IsType<RedirectToActionResult>(completeResult);
            var completedJob = await context.Jobs.FindAsync(editedJob.Id);
            Assert.Equal(JobStatus.Completed, completedJob!.Status);

            // ==========================================
            // WORKFLOW 5: Provider + Seeker Communication & Message History
            // ==========================================
            var messageController = new MessageController(context, userManager.Object);
            
            // 5.1 Provider sends a message to seeker
            messageController.ControllerContext = CreateControllerContext(providerUser, "JobProvider");
            var sendResult1 = await messageController.Send(editedJob.Id, "Hello Bob, please start with module A.");
            Assert.IsType<RedirectToActionResult>(sendResult1);

            // 5.2 Seeker sends a message to provider
            messageController.ControllerContext = CreateControllerContext(seekerUser, "JobSeeker");
            var sendResult2 = await messageController.Send(editedJob.Id, "Hi Alice, module A is complete and tested!");
            Assert.IsType<RedirectToActionResult>(sendResult2);

            // 5.3 Verify Message History & Unread Read-status
            var msgHistoryResult = await messageController.Index(editedJob.Id);
            var msgView = Assert.IsType<ViewResult>(msgHistoryResult);
            var msgModel = (MessageConversationViewModel)msgView.Model!;
            Assert.Equal(2, msgModel.Messages.Count);

            // ==========================================
            // WORKFLOW 6: Completed Job Payment Record, Rating, Review, Average Rating
            // ==========================================
            // 6.1 Payment Record Created Automatically on Completion
            var payment = await context.Payments.FirstOrDefaultAsync(p => p.JobId == editedJob.Id);
            Assert.NotNull(payment);
            Assert.Equal(PaymentStatus.Pending, payment.Status);
            Assert.Equal(600.00m, payment.Amount);

            // 6.2 Provider pays (Simulate Payment)
            var paymentController = new PaymentController(context, userManager.Object);
            paymentController.ControllerContext = CreateControllerContext(providerUser, "JobProvider");
            paymentController.TempData = new TempDataDictionary(paymentController.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var payResult = await paymentController.Pay(payment.Id);
            Assert.IsType<RedirectToActionResult>(payResult);
            var paidPayment = await context.Payments.FindAsync(payment.Id);
            Assert.Equal(PaymentStatus.Paid, paidPayment!.Status);
            Assert.NotNull(paidPayment.PaidAt);

            // 6.3 Provider Reviews Seeker
            var reviewController = new ReviewController(context, userManager.Object);
            reviewController.ControllerContext = CreateControllerContext(providerUser, "JobProvider");
            reviewController.TempData = new TempDataDictionary(reviewController.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var reviewResult1 = await reviewController.Create(editedJob.Id, 5, "Outstanding work! Fast and reliable.");
            Assert.IsType<RedirectToActionResult>(reviewResult1);

            // 6.4 Seeker Reviews Provider
            reviewController.ControllerContext = CreateControllerContext(seekerUser, "JobSeeker");
            reviewController.TempData = new TempDataDictionary(reviewController.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var reviewResult2 = await reviewController.Create(editedJob.Id, 4, "Clear specifications and prompt payment.");
            Assert.IsType<RedirectToActionResult>(reviewResult2);

            // 6.5 Verify Profile & Average Rating
            var profileController = new ProfileController(userManager.Object, context);
            profileController.ControllerContext = CreateControllerContext(seekerUser, "JobSeeker");

            var profileResult = await profileController.Index();
            var profileView = Assert.IsType<ViewResult>(profileResult);
            var profileModel = (ProfileViewModel)profileView.Model!;
            Assert.Equal(5.0, profileModel.AverageRating);
            Assert.Single(profileModel.Reviews);
        }

        [Fact]
        public async Task Security_UnauthorizedAccessAndOwnershipViolations_ArePrevented()
        {
            var context = CreateInMemoryDbContext();
            var users = new List<ApplicationUser>();
            var userManager = CreateMockUserManager(users);

            var notificationService = new NotificationService(context);
            var workflowService = new JobWorkflowService(context, notificationService);
            var assignmentService = new JobAssignmentService(context, notificationService);

            // Users: Provider 1, Provider 2, Seeker 1, Seeker 2
            var provider1 = new ApplicationUser { Id = "prov1", UserName = "provider1@test.com", Email = "provider1@test.com" };
            var provider2 = new ApplicationUser { Id = "prov2", UserName = "provider2@test.com", Email = "provider2@test.com" };
            var seeker1 = new ApplicationUser { Id = "seek1", UserName = "seeker1@test.com", Email = "seeker1@test.com" };
            var seeker2 = new ApplicationUser { Id = "seek2", UserName = "seeker2@test.com", Email = "seeker2@test.com" };
            users.AddRange(new[] { provider1, provider2, seeker1, seeker2 });

            // Create Job owned by Provider 1
            var job = new Job
            {
                Id = 101,
                Title = "Confidential Job",
                Description = "Security audit",
                Category = "Security",
                Deadline = DateTime.UtcNow.AddDays(7),
                PaymentAmount = 1000m,
                JobProviderId = provider1.Id,
                Status = JobStatus.Open
            };
            context.Jobs.Add(job);

            // Seeker 1 applies to Job 101
            var app1 = new JobApplication
            {
                Id = 201,
                JobId = job.Id,
                JobSeekerId = seeker1.Id,
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow
            };
            context.JobApplications.Add(app1);

            // Notification for Provider 1
            var notif1 = new Notification
            {
                Id = 301,
                UserId = provider1.Id,
                Title = "Private Alert",
                Message = "Your security token",
                IsRead = false
            };
            context.Notifications.Add(notif1);

            await context.SaveChangesAsync();

            // 1. IDOR: Provider 2 tries to edit Provider 1's job
            var jobController = new JobController(context, userManager.Object, workflowService);
            jobController.ControllerContext = CreateControllerContext(provider2, "JobProvider");
            jobController.TempData = new TempDataDictionary(jobController.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            var unauthorizedEditResult = await jobController.Edit(job.Id, new JobEditViewModel { Id = job.Id, Title = "Hacked Title" });
            Assert.IsType<NotFoundResult>(unauthorizedEditResult);

            // 2. IDOR: Provider 2 tries to view/accept application on Provider 1's job
            var providerAppController = new ProviderApplicationController(context, userManager.Object, assignmentService);
            providerAppController.ControllerContext = CreateControllerContext(provider2, "JobProvider");
            var unauthorizedAppDetails = await providerAppController.Details(app1.Id);
            Assert.IsType<ForbidResult>(unauthorizedAppDetails);

            // 3. IDOR: Seeker 2 tries to view or withdraw Seeker 1's application
            var appController = new ApplicationController(context, userManager.Object, notificationService);
            appController.ControllerContext = CreateControllerContext(seeker2, "JobSeeker");
            var unauthorizedSeekerAppDetails = await appController.Details(app1.Id);
            Assert.IsType<NotFoundResult>(unauthorizedSeekerAppDetails);

            // 4. IDOR: Seeker 2 tries to view notifications belonging to Provider 1
            var notifController = new NotificationController(notificationService, userManager.Object);
            notifController.ControllerContext = CreateControllerContext(seeker2, "JobSeeker");
            var seekerNotifications = await notificationService.GetUserNotificationsAsync(seeker2.Id);
            Assert.Empty(seekerNotifications);

            bool marked = await notificationService.MarkAsReadAsync(notif1.Id, seeker2.Id);
            Assert.False(marked);
            var originalNotif = await context.Notifications.FindAsync(notif1.Id);
            Assert.False(originalNotif!.IsRead); // untouched

            // 5. Duplicate Application Check
            appController.ControllerContext = CreateControllerContext(seeker1, "JobSeeker");
            appController.TempData = new TempDataDictionary(appController.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
            var dupApplyResult = await appController.Apply(new ApplyViewModel { JobId = job.Id, Message = "Second apply" });
            var dupApplyView = Assert.IsType<ViewResult>(dupApplyResult);
            Assert.False(appController.ModelState.IsValid);

            // 6. Invalid Status Transitions: Cannot edit a job once assigned or completed
            job.Status = JobStatus.Assigned;
            await context.SaveChangesAsync();

            jobController.ControllerContext = CreateControllerContext(provider1, "JobProvider");
            var editAssignedResult = await jobController.Edit(job.Id, new JobEditViewModel { Id = job.Id, Title = "Modified Assigned Job" });
            var editAssignedRedirect = Assert.IsType<RedirectToActionResult>(editAssignedResult);
            Assert.Equal("Index", editAssignedRedirect.ActionName);
            Assert.Equal("Only open jobs can be edited.", jobController.TempData["ErrorMessage"]);

            // 7. Duplicate Assignment Prevention: Cannot accept another application if job is already assigned
            var app2 = new JobApplication
            {
                Id = 202,
                JobId = job.Id,
                JobSeekerId = seeker2.Id,
                Status = ApplicationStatus.Pending
            };
            context.JobApplications.Add(app2);
            await context.SaveChangesAsync();

            var dupAccept = await assignmentService.AcceptApplicationAsync(app2.Id, provider1.Id);
            Assert.False(dupAccept.Success);
            Assert.Contains("no longer open", dupAccept.Message);

            // 8. Unauthorized Messaging: Seeker 2 (not assigned) tries to message on Job 101
            var messageController = new MessageController(context, userManager.Object);
            messageController.ControllerContext = CreateControllerContext(seeker2, "JobSeeker");
            var unauthorizedMsgResult = await messageController.Index(job.Id);
            Assert.IsType<ForbidResult>(unauthorizedMsgResult);
        }
    }
}
