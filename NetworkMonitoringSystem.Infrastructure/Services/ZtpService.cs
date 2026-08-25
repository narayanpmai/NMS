using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Infrastructure.Data;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Services
{
    public class ZtpService : IZtpService
    {
        private readonly ApplicationDbContext _context;

        public ZtpService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateConfigurationAsync(string macAddress)
        {
            var profile = await _context.ZtpProfiles
                .Include(p => p.Template)
                .FirstOrDefaultAsync(p => p.MacAddress.ToLower() == macAddress.ToLower());

            if (profile == null)
            {
                return null;
            }

            string config = profile.Template.Content;

            if (!string.IsNullOrWhiteSpace(profile.VariablesJson))
            {
                try
                {
                    var variables = JsonSerializer.Deserialize<Dictionary<string, string>>(profile.VariablesJson);
                    if (variables != null)
                    {
                        foreach (var kvp in variables)
                        {
                            config = config.Replace("{{" + kvp.Key + "}}", kvp.Value);
                        }
                    }
                }
                catch
                {
                    // If JSON is invalid, return the raw template or handle it
                }
            }

            return config;
        }
    }
}
