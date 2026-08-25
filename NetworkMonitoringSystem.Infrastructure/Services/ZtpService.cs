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

            // Auto-onboarding: Check if device already exists
            var existingDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.MacAddress != null && d.MacAddress.ToLower() == macAddress.ToLower());
                
            if (existingDevice == null)
            {
                var variables = !string.IsNullOrWhiteSpace(profile.VariablesJson) 
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(profile.VariablesJson) 
                    : new Dictionary<string, string>();
                    
                var deviceType = await _context.DeviceTypes.FirstOrDefaultAsync(t => t.Name == "Switch") ?? 
                                 await _context.DeviceTypes.FirstOrDefaultAsync();
                var status = await _context.DeviceStatuses.FirstOrDefaultAsync(s => s.Name == "Online") ?? 
                             await _context.DeviceStatuses.FirstOrDefaultAsync();
                var location = await _context.Locations.FirstOrDefaultAsync(l => l.Name == "Auto Discovered Subnet");

                var newDevice = new NetworkMonitoringSystem.Domain.Entities.Device
                {
                    Name = variables.ContainsKey("HOSTNAME") ? variables["HOSTNAME"] : "ZTP-" + macAddress,
                    Hostname = variables.ContainsKey("HOSTNAME") ? variables["HOSTNAME"] : "ZTP-" + macAddress,
                    IPAddress = variables.ContainsKey("IP_ADDRESS") ? variables["IP_ADDRESS"] : "0.0.0.0",
                    MacAddress = macAddress,
                    DeviceTypeId = deviceType?.Id ?? 0,
                    StatusId = status?.Id ?? 0,
                    Location = location?.Name,
                    DesiredConfiguration = profile.Template.Content, // Link desired configuration to the ZTP template
                    IsConfigCompliant = true
                };

                _context.Devices.Add(newDevice);
                await _context.SaveChangesAsync();
            }

            return config;
        }
    }
}
