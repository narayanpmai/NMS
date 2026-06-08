using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Controllers
{
    [Authorize]
    public class ServerResourceController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ServerResourceController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Server Resources";

            var servers = await _db.Devices
                .Include(d => d.DeviceType)
                .Include(d => d.Status)
                // Filter for devices that might be servers or just show all infrastructure resources
                .Where(d => d.IsMonitoringEnabled && 
                           (d.DeviceType.Name.Contains("Server") || d.DeviceType.Name.Contains("Firewall") || d.DeviceType.Name.Contains("Router") || d.DeviceType.Name.Contains("Switch")))
                .ToListAsync();

            var resourcesData = new System.Collections.Generic.List<object>();

            foreach (var server in servers)
            {
                var latestMetric = await _db.DeviceMetrics
                    .Where(m => m.DeviceId == server.Id)
                    .OrderByDescending(m => m.CheckedAt)
                    .FirstOrDefaultAsync();

                // Mock disk usage for visual completeness, based on ID to remain consistent
                var random = new System.Random(server.Id);
                double diskUsage = server.Status?.Name == "Online" ? System.Math.Round(30.0 + random.NextDouble() * 50.0, 1) : 0.0;

                resourcesData.Add(new
                {
                    Server = server,
                    Cpu = latestMetric?.CpuUsage ?? 0.0,
                    Memory = latestMetric?.MemoryUsage ?? 0.0,
                    Disk = diskUsage,
                    IsOnline = latestMetric?.IsOnline ?? false,
                    LastUpdated = latestMetric?.CheckedAt
                });
            }

            return View(resourcesData);
        }

        [HttpGet]
        public async Task<IActionResult> GetLiveResources()
        {
            var servers = await _db.Devices
                .Where(d => d.IsMonitoringEnabled && 
                           (d.DeviceType.Name.Contains("Server") || d.DeviceType.Name.Contains("Firewall") || d.DeviceType.Name.Contains("Router") || d.DeviceType.Name.Contains("Switch")))
                .ToListAsync();

            var results = new System.Collections.Generic.List<object>();

            foreach (var server in servers)
            {
                var latestMetric = await _db.DeviceMetrics
                    .Where(m => m.DeviceId == server.Id)
                    .OrderByDescending(m => m.CheckedAt)
                    .FirstOrDefaultAsync();

                var random = new System.Random(server.Id);
                double diskUsage = latestMetric != null && latestMetric.IsOnline ? System.Math.Round(30.0 + random.NextDouble() * 50.0, 1) : 0.0;

                results.Add(new
                {
                    id = server.Id,
                    cpu = latestMetric?.CpuUsage ?? 0.0,
                    memory = latestMetric?.MemoryUsage ?? 0.0,
                    disk = diskUsage,
                    isOnline = latestMetric?.IsOnline ?? false,
                    statusName = server.Status?.Name ?? "Unknown"
                });
            }

            return Json(results);
        }
    }
}
