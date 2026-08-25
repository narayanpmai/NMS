using Microsoft.AspNetCore.Mvc;
using NetworkMonitoringSystem.Application.Interfaces;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Controllers
{
    [ApiController]
    [Route("api/ztp")]
    public class ZtpApiController : ControllerBase
    {
        private readonly IZtpService _ztpService;

        public ZtpApiController(IZtpService ztpService)
        {
            _ztpService = ztpService;
        }

        [HttpGet("config/{mac}")]
        public async Task<IActionResult> GetConfig(string mac)
        {
            if (string.IsNullOrWhiteSpace(mac))
            {
                return BadRequest("MAC address is required.");
            }

            var config = await _ztpService.GenerateConfigurationAsync(mac);

            if (config == null)
            {
                return NotFound($"No ZTP profile found for MAC: {mac}");
            }

            return Content(config, "text/plain");
        }
    }
}
