using OnlineJobAssignment.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message);
        Task<int> GetUnreadCountAsync(string userId);
        Task<List<Notification>> GetUserNotificationsAsync(string userId);
        Task<bool> MarkAsReadAsync(int notificationId, string userId);
    }
}
