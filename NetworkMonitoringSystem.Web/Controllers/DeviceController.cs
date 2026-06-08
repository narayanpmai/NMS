using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Domain.Entities;
using NetworkMonitoringSystem.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Controllers
{
    [Authorize]
    public class DeviceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDiscoveryService _discoveryService;

        public DeviceController(ApplicationDbContext context, IDiscoveryService discoveryService)
        {
            _context = context;
            _discoveryService = discoveryService;
        }

        public async Task<IActionResult> Index()
        {
            var devices = await _context.Devices
                .Include(d => d.DeviceType)
                .Include(d => d.Status)
                .ToListAsync();
            return View(devices);
        }

        // ── Discovery ────────────────────────────────────────────
        public IActionResult Discover()
        {
            ViewData["Title"] = "Auto-Discovery Scan";

            string defaultSubnet = "192.168.1";
            try
            {
                foreach (var netInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (netInterface.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                        netInterface.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    {
                        var ipProps = netInterface.GetIPProperties();
                        var ipv4 = ipProps.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                        if (ipv4 != null)
                        {
                            var parts = ipv4.Address.ToString().Split('.');
                            if (parts.Length == 4)
                            {
                                defaultSubnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

            ViewBag.DefaultSubnet = defaultSubnet;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ScanSubnet([FromForm] string subnet, [FromForm] int startIp, [FromForm] int endIp)
        {
            if (string.IsNullOrWhiteSpace(subnet))
                return BadRequest("Subnet is required.");

            var discovered = await _discoveryService.ScanIPRangeAsync(subnet, startIp, endIp);
            var existingIPs = await _context.Devices.Select(d => d.IPAddress).ToListAsync();

            var results = discovered.Select(d => new
            {
                name        = d.Name,
                hostname    = d.Hostname,
                ipAddress   = d.IPAddress,
                macAddress  = d.MacAddress,
                vendor      = d.Vendor,
                model       = d.Model,
                location    = d.Location,
                isNew       = !existingIPs.Contains(d.IPAddress)
            }).ToList();

            return Json(results);
        }

        [HttpPost]
        public async Task<IActionResult> OnboardDevice([FromForm] string name, [FromForm] string hostname,
            [FromForm] string ipAddress, [FromForm] string macAddress, [FromForm] string vendor, [FromForm] string model)
        {
            if (await _context.Devices.AnyAsync(d => d.IPAddress == ipAddress))
            {
                TempData["ErrorMessage"] = $"Device with IP {ipAddress} already exists.";
                return RedirectToAction(nameof(Discover));
            }

            var defaultType   = await _context.DeviceTypes.FirstOrDefaultAsync();
            var defaultStatus = await _context.DeviceStatuses.FirstOrDefaultAsync();

            var device = new Device
            {
                Name        = name,
                Hostname    = hostname,
                IPAddress   = ipAddress,
                MacAddress  = macAddress ?? "",
                Vendor      = vendor    ?? "Generic",
                Model       = model     ?? "Unknown",
                Location    = "Auto Discovered",
                Department  = "Network Discovery",
                ContactPerson = "",
                DeviceTypeId  = defaultType?.Id  ?? 1,
                StatusId      = defaultStatus?.Id ?? 1,
                IsMonitoringEnabled = true
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Device '{name}' ({ipAddress}) onboarded successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ── CRUD ─────────────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            await PopulateDropDowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Hostname,IPAddress,MacAddress,Vendor,Model,Location,Department,DeviceTypeId,StatusId")] Device device)
        {
            ModelState.Remove(nameof(Device.DeviceType));
            ModelState.Remove(nameof(Device.Status));
            ModelState.Remove(nameof(Device.CreatedBy));
            ModelState.Remove(nameof(Device.LastModifiedBy));
            ModelState.Remove(nameof(Device.ContactPerson));
            ModelState.Remove(nameof(Device.Project));

            if (ModelState.IsValid)
            {
                device.ContactPerson = "";
                device.MacAddress    ??= "";
                device.Vendor        ??= "";
                device.Model         ??= "";
                device.Location      ??= "";
                device.Department    ??= "";
                device.IsMonitoringEnabled = true;

                _context.Add(device);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropDowns();
            return View(device);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();
            await PopulateDropDowns();
            return View(device);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Hostname,IPAddress,MacAddress,Vendor,Model,Location,Department,DeviceTypeId,StatusId")] Device device)
        {
            if (id != device.Id) return NotFound();

            ModelState.Remove(nameof(Device.DeviceType));
            ModelState.Remove(nameof(Device.Status));
            ModelState.Remove(nameof(Device.CreatedBy));
            ModelState.Remove(nameof(Device.LastModifiedBy));
            ModelState.Remove(nameof(Device.ContactPerson));
            ModelState.Remove(nameof(Device.Project));

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDevice = await _context.Devices.FindAsync(id);
                    if (existingDevice == null) return NotFound();

                    existingDevice.Name         = device.Name;
                    existingDevice.Hostname     = device.Hostname;
                    existingDevice.IPAddress    = device.IPAddress;
                    existingDevice.MacAddress   = device.MacAddress ?? "";
                    existingDevice.Vendor       = device.Vendor     ?? "";
                    existingDevice.Model        = device.Model      ?? "";
                    existingDevice.Location     = device.Location   ?? "";
                    existingDevice.Department   = device.Department ?? "";
                    existingDevice.DeviceTypeId = device.DeviceTypeId;
                    existingDevice.StatusId     = device.StatusId;

                    _context.Update(existingDevice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeviceExists(device.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropDowns();
            return View(device);
        }

        // ── Delete ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();

            // Get alert IDs for this device
            var alertIds = await _context.Alerts
                .Where(a => a.DeviceId == id)
                .Select(a => a.Id)
                .ToListAsync();

            // Remove incidents linked to those alerts first (FK constraint)
            if (alertIds.Any())
            {
                var incidents = _context.Incidents.Where(i => i.AlertId.HasValue && alertIds.Contains(i.AlertId.Value));
                _context.Incidents.RemoveRange(incidents);
            }

            // Also remove incidents linked directly to the device
            var deviceIncidents = _context.Incidents.Where(i => i.DeviceId == id);
            _context.Incidents.RemoveRange(deviceIncidents);

            // Now remove alerts, metrics, and the device
            var alerts = _context.Alerts.Where(a => a.DeviceId == id);
            var metrics = _context.DeviceMetrics.Where(m => m.DeviceId == id);
            _context.Alerts.RemoveRange(alerts);
            _context.DeviceMetrics.RemoveRange(metrics);

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Device '{device.Name}' has been deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── Toggle Monitoring (Enable / Disable) ──────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMonitoring(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null) return NotFound();

            device.IsMonitoringEnabled = !device.IsMonitoringEnabled;
            await _context.SaveChangesAsync();

            var status = device.IsMonitoringEnabled ? "enabled" : "disabled";
            TempData["SuccessMessage"] = $"Monitoring for '{device.Name}' has been {status}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropDowns()
        {
            ViewBag.DeviceTypes   = await _context.DeviceTypes.ToListAsync();
            ViewBag.DeviceStatuses = await _context.DeviceStatuses.ToListAsync();
        }

        private bool DeviceExists(int id) => _context.Devices.Any(e => e.Id == id);
    }
}
