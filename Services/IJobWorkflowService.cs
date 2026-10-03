using System.Threading.Tasks;

namespace OnlineJobAssignment.Services
{
    public interface IJobWorkflowService
    {
        Task<(bool Success, string Message)> StartJobAsync(int jobId, string userId);
        Task<(bool Success, string Message)> CompleteJobAsync(int jobId, string userId);
        Task<(bool Success, string Message)> CancelJobAsync(int jobId, string userId);
    }
}
