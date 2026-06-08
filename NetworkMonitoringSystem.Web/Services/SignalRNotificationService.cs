using Microsoft.AspNetCore.SignalR;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Web.Hubs;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Services
{
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<MonitoringHub> _hubContext;

        public SignalRNotificationService(IHubContext<MonitoringHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendStatusUpdateAsync(int deviceId, string deviceName, string status, double responseTime, double cpuUsage, double memoryUsage)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", deviceId, deviceName, status, responseTime, cpuUsage, memoryUsage);
        }

        public async Task SendDashboardUpdateAsync(int totalDevices, int online, int warning, int offline)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveDashboardUpdate", totalDevices, online, warning, offline);
        }

        public async Task SendAlertAsync(string deviceName, string message, string level)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveAlert", deviceName, message, level);
        }
    }
}
