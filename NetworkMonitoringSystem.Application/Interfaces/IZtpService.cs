using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Application.Interfaces
{
    public interface IZtpService
    {
        Task<string> GenerateConfigurationAsync(string macAddress);
    }
}
