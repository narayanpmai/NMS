using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Application.Interfaces
{
    public interface IPingService
    {
        Task ProcessPingChecksAsync(CancellationToken cancellationToken);
        Task CalculateMonthlySlaAsync(CancellationToken cancellationToken);
    }
}
