using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendStatusUpdateAsync(int deviceId, string deviceName, string status, double responseTime, double cpuUsage, double memoryUsage);
        Task SendDashboardUpdateAsync(int totalDevices, int online, int warning, int offline);
        Task SendAlertAsync(string deviceName, string message, string level);
    }
}
