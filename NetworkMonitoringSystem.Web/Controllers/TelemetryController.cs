using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [AllowAnonymous] // Assuming we might want to expose this with a different auth mechanism later
    public class TelemetryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TelemetryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("devices")]
        public async Task<IActionResult> GetDevices()
        {
            var devices = await _context.Devices
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.IPAddress,
                    Status = d.Status.Name,
                    d.Model,
                    d.Vendor,
                    d.IsMonitoringEnabled
                })
                .ToListAsync();

            return Ok(devices);
        }
        
        [HttpGet("devices/{id}/metrics")]
        public async Task<IActionResult> GetDeviceMetrics(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound(new { error = "Device not found" });
            }

            var metrics = await _context.DeviceMetrics
                .Where(m => m.DeviceId == id)
                .OrderByDescending(m => m.CheckedAt)
                .Take(50)
                .ToListAsync();

            return Ok(metrics);
        }
    }
}
