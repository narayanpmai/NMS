using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Application.Interfaces
{
    public interface IConfigurationComplianceService
    {
        Task CheckComplianceAsync();
    }
}
