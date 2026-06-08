using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Hubs
{
    public class MonitoringHub : Hub
    {
        public async Task SendStatusUpdate(int deviceId, string deviceName, string status, double responseTime, double cpuUsage, double memoryUsage)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", deviceId, deviceName, status, responseTime, cpuUsage, memoryUsage);
        }

        public async Task SendDashboardUpdate(int totalDevices, int online, int warning, int offline)
        {
            await Clients.All.SendAsync("ReceiveDashboardUpdate", totalDevices, online, warning, offline);
        }

        public async Task SendAlert(string deviceName, string message, string level)
        {
            await Clients.All.SendAsync("ReceiveAlert", deviceName, message, level);
        }
    }
}
