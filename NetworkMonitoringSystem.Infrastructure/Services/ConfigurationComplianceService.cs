using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Domain.Entities;
using NetworkMonitoringSystem.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Services
{
    public class ConfigurationComplianceService : IConfigurationComplianceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConfigurationComplianceService> _logger;
        private static readonly Random _random = new Random();

        public ConfigurationComplianceService(ApplicationDbContext context, ILogger<ConfigurationComplianceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CheckComplianceAsync()
        {
            _logger.LogInformation("Starting Configuration Compliance Check...");

            var devices = await _context.Devices
                .Where(d => !string.IsNullOrEmpty(d.DesiredConfiguration))
                .ToListAsync();

            foreach (var device in devices)
            {
                // Simulate pulling the running configuration via SSH/NETCONF
                string simulatedRunningConfig = GenerateSimulatedConfig(device);

                // Check for drift (compliance)
                // We do a simple check: if the running config doesn't perfectly contain the desired config
                bool isCompliant = simulatedRunningConfig.Contains(device.DesiredConfiguration.Trim());

                // Randomly cause a drift for demonstration purposes occasionally (10% chance)
                if (isCompliant && _random.Next(1, 100) <= 10)
                {
                    isCompliant = false;
                    simulatedRunningConfig = simulatedRunningConfig.Replace(device.DesiredConfiguration.Trim(), "hostname unauthorized-change\n!");
                    _logger.LogWarning("Simulated Configuration Drift detected on {DeviceName}!", device.Name);
                }

                device.IsConfigCompliant = isCompliant;
                _context.Update(device);

                var backup = new ConfigurationBackup
                {
                    DeviceId = device.Id,
                    ConfigContent = simulatedRunningConfig,
                    IsCompliant = isCompliant,
                    ConfigVersion = DateTime.UtcNow.ToString("yyyyMMdd-HHmm")
                };

                _context.ConfigurationBackups.Add(backup);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Configuration Compliance Check completed.");
        }

        private string GenerateSimulatedConfig(Device device)
        {
            // Start with standard boilerplate
            string config = $"!\n! Last configuration change at {DateTime.UtcNow:O}\n!\nversion 15.2\n";
            config += $"hostname {device.Hostname ?? device.Name}\n!\n";
            
            // Add the intended configuration to simulate it's currently correct
            config += $"{device.DesiredConfiguration}\n!\n";
            
            config += $"interface GigabitEthernet0/0\n ip address {device.IPAddress} 255.255.255.0\n!\n";
            config += "line vty 0 4\n login local\n transport input ssh\n!\nend";

            return config;
        }
    }
}
