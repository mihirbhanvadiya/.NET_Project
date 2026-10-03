using System.Threading.Tasks;

namespace OnlineJobAssignment.Services
{
    public interface IJobAssignmentService
    {
        Task<(bool Success, string Message)> AcceptApplicationAsync(int applicationId, string providerId);
        Task<(bool Success, string Message)> RejectApplicationAsync(int applicationId, string providerId);
    }
}
