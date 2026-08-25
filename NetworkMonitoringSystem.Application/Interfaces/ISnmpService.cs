using System.Threading.Tasks;
using NetworkMonitoringSystem.Domain.Entities;

namespace NetworkMonitoringSystem.Application.Interfaces
{
    public interface ISnmpService
    {
        Task PollDevicesAsync();
    }
}
