namespace OnlineJobAssignment.ViewModels
{
    public class SeekerDashboardViewModel
    {
        public int AvailableJobs { get; set; }
        public int MyApplications { get; set; }
        public int PendingApplications { get; set; }
        public int AcceptedApplications { get; set; }
        public int AssignedJobs { get; set; }
        public int CompletedJobs { get; set; }
        public System.Collections.Generic.List<OnlineJobAssignment.Models.Job> RecentAssignedJobs { get; set; } = new();
        public System.Collections.Generic.List<OnlineJobAssignment.Models.Notification> RecentNotifications { get; set; } = new();
        public System.Collections.Generic.List<OnlineJobAssignment.Models.Message> RecentMessages { get; set; } = new();
    }
}
