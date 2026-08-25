using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Application.Interfaces
{
    public interface ITopologyDiscoveryService
    {
        Task DiscoverTopologyAsync();
    }
}
