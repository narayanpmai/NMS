using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Application.Interfaces
{
    public interface IISPMonitoringService
    {
        Task ProcessISPChecksAsync(CancellationToken cancellationToken = default);
    }
}
