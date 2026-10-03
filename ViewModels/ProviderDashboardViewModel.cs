namespace OnlineJobAssignment.ViewModels
{
    public class ProviderDashboardViewModel
    {
        public int TotalJobs { get; set; }
        public int OpenJobs { get; set; }
        public int AssignedJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int PendingApplications { get; set; }
        public System.Collections.Generic.List<OnlineJobAssignment.Models.JobApplication> RecentApplications { get; set; } = new();
        public System.Collections.Generic.List<OnlineJobAssignment.Models.Notification> RecentNotifications { get; set; } = new();
        public System.Collections.Generic.List<OnlineJobAssignment.Models.Message> RecentMessages { get; set; } = new();
    }
}
