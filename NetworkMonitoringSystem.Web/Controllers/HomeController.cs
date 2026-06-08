using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Infrastructure.Data;
using NetworkMonitoringSystem.Web.Models;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch real device counts
            var devices = await _context.Devices
                .Include(d => d.DeviceType)
                .Include(d => d.Status)
                .ToListAsync();

            var onlineCount = devices.Count(d => d.Status?.Name == "Online");
            var offlineCount = devices.Count(d => d.Status?.Name == "Offline");
            var warningCount = devices.Count(d => d.Status?.Name == "Warning");

            ViewBag.TotalDevices = devices.Count;
            ViewBag.OnlineDevices = onlineCount;
            ViewBag.OfflineDevices = offlineCount;
            ViewBag.WarningDevices = warningCount;
            ViewBag.UptimePercent = devices.Count > 0
                ? ((double)onlineCount / devices.Count * 100).ToString("F1")
                : "0.0";

            // Fetch recent alerts
            var recentAlerts = await _context.Alerts
                .Include(a => a.Device)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentAlerts = recentAlerts;

            // Devices with latest metrics for the status table
            var devicesWithMetrics = new System.Collections.Generic.List<dynamic>();
            foreach (var device in devices)
            {
                var lastMetric = await _context.DeviceMetrics
                    .Where(m => m.DeviceId == device.Id)
                    .OrderByDescending(m => m.CheckedAt)
                    .FirstOrDefaultAsync();

                devicesWithMetrics.Add(new { Device = device, LastMetric = lastMetric });
            }
            ViewBag.DevicesWithMetrics = devicesWithMetrics;

            // Chart data - average response times from last 20 metric cycles
            var rawMetrics = await _context.DeviceMetrics
                .OrderByDescending(m => m.CheckedAt)
                .Take(100)
                .ToListAsync();

            var chartMetrics = rawMetrics
                .GroupBy(m => m.CheckedAt.ToString("HH:mm"))
                .Select(g => new { Time = g.Key, AvgResponse = g.Average(m => m.ResponseTimeMs) })
                .Take(20)
                .ToList();

            chartMetrics.Reverse();
            ViewBag.ChartLabels = chartMetrics.Select(c => c.Time).ToArray();
            ViewBag.ChartData = chartMetrics.Select(c => Math.Round(c.AvgResponse, 1)).ToArray();

            return View();
        }

        public IActionResult Map()
        {
            ViewData["Title"] = "GIS Map Monitoring";
            return View();
        }

        public IActionResult Topology()
        {
            ViewData["Title"] = "Network Topology Map";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMapData()
        {
            var devices = await _context.Devices
                .Include(d => d.Status)
                .Include(d => d.DeviceType)
                .ToListAsync();

            var locations = await _context.Locations.ToListAsync();

            var mapData = new System.Collections.Generic.List<object>();

            foreach (var d in devices)
            {
                var latestMetric = await _context.DeviceMetrics
                    .Where(m => m.DeviceId == d.Id)
                    .OrderByDescending(m => m.CheckedAt)
                    .FirstOrDefaultAsync();

                double lat = 27.7172;
                double lng = 85.3240;
                string locName = d.Location ?? "Default Location";

                var matchedLoc = locations.FirstOrDefault(l => !string.IsNullOrEmpty(d.Location) && 
                    (d.Location.Contains(l.Name, StringComparison.OrdinalIgnoreCase) || 
                     l.Name.Contains(d.Location, StringComparison.OrdinalIgnoreCase)));

                if (matchedLoc != null)
                {
                    lat = matchedLoc.Latitude;
                    lng = matchedLoc.Longitude;
                    locName = matchedLoc.Name;
                }
                else
                {
                    var random = new Random(d.Id);
                    lat += (random.NextDouble() - 0.5) * 0.05;
                    lng += (random.NextDouble() - 0.5) * 0.05;
                }

                mapData.Add(new
                {
                    id = d.Id,
                    name = d.Name,
                    ipAddress = d.IPAddress,
                    status = d.Status?.Name ?? "Unknown",
                    statusColor = d.Status?.ColorCode ?? "secondary",
                    latitude = lat,
                    longitude = lng,
                    locationName = locName,
                    cpuUsage = latestMetric?.CpuUsage ?? 0.0,
                    memoryUsage = latestMetric?.MemoryUsage ?? 0.0,
                    responseTime = latestMetric?.ResponseTimeMs ?? 0.0
                });
            }

            return Json(mapData);
        }

        [HttpGet]
        public async Task<IActionResult> GetTopologyData()
        {
            var devices = await _context.Devices
                .Include(d => d.Status)
                .Include(d => d.DeviceType)
                .ToListAsync();

            var nodes = new System.Collections.Generic.List<object>();
            var links = new System.Collections.Generic.List<object>();

            if (!devices.Any())
            {
                return Json(new { nodes, links });
            }

            foreach (var d in devices)
            {
                var latestMetric = await _context.DeviceMetrics
                    .Where(m => m.DeviceId == d.Id)
                    .OrderByDescending(m => m.CheckedAt)
                    .FirstOrDefaultAsync();

                nodes.Add(new
                {
                    id = d.Id.ToString(),
                    name = d.Name,
                    ip = d.IPAddress,
                    type = d.DeviceType?.Name ?? "Server",
                    status = d.Status?.Name ?? "Unknown",
                    statusColor = d.Status?.ColorCode ?? "secondary",
                    cpu = latestMetric?.CpuUsage ?? 0.0,
                    memory = latestMetric?.MemoryUsage ?? 0.0
                });
            }

            var routers = devices.Where(d => d.DeviceType?.Name == "Router" || d.Name.Contains("Router", StringComparison.OrdinalIgnoreCase)).ToList();
            var firewalls = devices.Where(d => d.DeviceType?.Name == "Firewall" || d.Name.Contains("Firewall", StringComparison.OrdinalIgnoreCase)).ToList();
            var switches = devices.Where(d => d.DeviceType?.Name == "Switch" || d.Name.Contains("Switch", StringComparison.OrdinalIgnoreCase)).ToList();
            var servers = devices.Where(d => d.DeviceType?.Name == "Server" || d.DeviceType?.Name == "Workstation" || (!routers.Contains(d) && !firewalls.Contains(d) && !switches.Contains(d))).ToList();

            var root = routers.FirstOrDefault() ?? firewalls.FirstOrDefault() ?? switches.FirstOrDefault() ?? devices.First();

            foreach (var r in routers)
            {
                if (r.Id != root.Id)
                {
                    links.Add(new { source = root.Id.ToString(), target = r.Id.ToString(), label = "WAN Link" });
                }
            }

            foreach (var fw in firewalls)
            {
                if (fw.Id != root.Id)
                {
                    var parent = routers.FirstOrDefault() ?? root;
                    links.Add(new { source = parent.Id.ToString(), target = fw.Id.ToString(), label = "Security Link" });
                }
            }

            foreach (var sw in switches)
            {
                if (sw.Id != root.Id)
                {
                    var parent = firewalls.FirstOrDefault() ?? routers.FirstOrDefault() ?? root;
                    links.Add(new { source = parent.Id.ToString(), target = sw.Id.ToString(), label = "Trunk Link" });
                }
            }

            foreach (var s in servers)
            {
                if (s.Id != root.Id)
                {
                    var parent = switches.FirstOrDefault() ?? root;
                    links.Add(new { source = parent.Id.ToString(), target = s.Id.ToString(), label = "Access Link" });
                }
            }

            return Json(new { nodes, links });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
