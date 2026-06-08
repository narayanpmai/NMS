using NetworkMonitoringSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Application.Interfaces
{
    public interface IDiscoveryService
    {
        Task<List<Device>> ScanIPRangeAsync(string subnet, int startIP, int endIP);
    }
}
