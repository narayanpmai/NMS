using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Domain.Entities;
using NetworkMonitoringSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Services
{
    public class TopologyDiscoveryService : ITopologyDiscoveryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TopologyDiscoveryService> _logger;

        public TopologyDiscoveryService(ApplicationDbContext context, ILogger<TopologyDiscoveryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task DiscoverTopologyAsync()
        {
            _logger.LogInformation("Starting Topology Discovery...");

            var devices = await _context.Devices
                .Include(d => d.DeviceType)
                .ToListAsync();

            if (!devices.Any())
            {
                _logger.LogWarning("No devices found for topology discovery.");
                return;
            }

            // Remove existing auto-generated links to recreate them (simulate LLDP mapping)
            var existingLinks = await _context.DeviceLinks.ToListAsync();
            _context.DeviceLinks.RemoveRange(existingLinks);
            await _context.SaveChangesAsync();

            var routers = devices.Where(d => d.DeviceType?.Name == "Router" || d.Name.Contains("Router", StringComparison.OrdinalIgnoreCase)).ToList();
            var firewalls = devices.Where(d => d.DeviceType?.Name == "Firewall" || d.Name.Contains("Firewall", StringComparison.OrdinalIgnoreCase)).ToList();
            var switches = devices.Where(d => d.DeviceType?.Name == "Switch" || d.Name.Contains("Switch", StringComparison.OrdinalIgnoreCase)).ToList();
            var servers = devices.Where(d => d.DeviceType?.Name == "Server" || d.DeviceType?.Name == "Workstation" || (!routers.Contains(d) && !firewalls.Contains(d) && !switches.Contains(d))).ToList();

            var root = routers.FirstOrDefault() ?? firewalls.FirstOrDefault() ?? switches.FirstOrDefault() ?? devices.First();

            var newLinks = new List<DeviceLink>();
            int portIndex = 1;

            foreach (var r in routers)
            {
                if (r.Id != root.Id)
                {
                    newLinks.Add(new DeviceLink
                    {
                        SourceDeviceId = root.Id,
                        TargetDeviceId = r.Id,
                        SourcePort = $"Gi0/{portIndex++}",
                        TargetPort = "Gi0/1",
                        LinkType = "WAN Link",
                        BandwidthMbps = 10000
                    });
                }
            }

            foreach (var fw in firewalls)
            {
                if (fw.Id != root.Id)
                {
                    var parent = routers.FirstOrDefault() ?? root;
                    newLinks.Add(new DeviceLink
                    {
                        SourceDeviceId = parent.Id,
                        TargetDeviceId = fw.Id,
                        SourcePort = $"Gi0/{portIndex++}",
                        TargetPort = "eth0",
                        LinkType = "Security Link",
                        BandwidthMbps = 1000
                    });
                }
            }

            foreach (var sw in switches)
            {
                if (sw.Id != root.Id)
                {
                    var parent = firewalls.FirstOrDefault() ?? routers.FirstOrDefault() ?? root;
                    newLinks.Add(new DeviceLink
                    {
                        SourceDeviceId = parent.Id,
                        TargetDeviceId = sw.Id,
                        SourcePort = $"Te1/1/1",
                        TargetPort = $"Te1/1/{portIndex++}",
                        LinkType = "Trunk Link",
                        BandwidthMbps = 10000
                    });
                }
            }

            foreach (var s in servers)
            {
                if (s.Id != root.Id)
                {
                    var parent = switches.FirstOrDefault() ?? root;
                    newLinks.Add(new DeviceLink
                    {
                        SourceDeviceId = parent.Id,
                        TargetDeviceId = s.Id,
                        SourcePort = $"Fa0/{portIndex++}",
                        TargetPort = "eth0",
                        LinkType = "Access Link",
                        BandwidthMbps = 1000
                    });
                }
            }

            await _context.DeviceLinks.AddRangeAsync(newLinks);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Topology Discovery completed successfully. {LinkCount} links established.", newLinks.Count);
        }
    }
}
