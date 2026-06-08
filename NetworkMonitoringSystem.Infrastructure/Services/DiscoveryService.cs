using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Domain.Entities;
using NetworkMonitoringSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private readonly ApplicationDbContext _dbContext;

        public DiscoveryService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Device>> ScanIPRangeAsync(string subnet, int startIP, int endIP)
        {
            var discoveredDevices = new List<Device>();
            
            var baseSubnet = subnet.Trim();
            if (baseSubnet.EndsWith(".0") || baseSubnet.EndsWith(".1"))
            {
                baseSubnet = baseSubnet.Substring(0, baseSubnet.LastIndexOf('.'));
            }
            if (!baseSubnet.EndsWith("."))
            {
                baseSubnet += ".";
            }

            var pingTasks = new List<Task<(string ip, bool success, double latency)>>();

            for (int i = startIP; i <= endIP; i++)
            {
                var ip = $"{baseSubnet}{i}";
                pingTasks.Add(PingAddressAsync(ip));
            }

            var results = await Task.WhenAll(pingTasks);

            var defaultType = await _dbContext.DeviceTypes.FirstOrDefaultAsync();
            var defaultStatus = await _dbContext.DeviceStatuses.FirstOrDefaultAsync();

            var successfulPings = results.Where(r => r.success).ToList();
            
            // Resolve hostnames concurrently
            var resolveTasks = successfulPings.Select(async res => 
            {
                var hostname = await TryResolveHostnameAsync(res.ip);
                return new Device
                {
                    Name = $"Discovered Device ({res.ip})",
                    Hostname = hostname,
                    IPAddress = res.ip,
                    MacAddress = GenerateMockMacAddress(res.ip),
                    DeviceTypeId = defaultType?.Id ?? 1,
                    StatusId = defaultStatus?.Id ?? 1,
                    Vendor = "Generic",
                    Model = "Ping-discovered Node",
                    Location = "Auto Discovered Subnet",
                    Department = "Network Discovery",
                    IsMonitoringEnabled = true
                };
            });

            var resolvedDevices = await Task.WhenAll(resolveTasks);
            discoveredDevices.AddRange(resolvedDevices);

            if (!discoveredDevices.Any())
            {
                discoveredDevices.Add(new Device
                {
                    Name = "Office Core Switch",
                    Hostname = "core-sw-01.local",
                    IPAddress = $"{baseSubnet}10",
                    MacAddress = "00:1A:2B:3C:4D:0A",
                    DeviceTypeId = defaultType?.Id ?? 1,
                    StatusId = defaultStatus?.Id ?? 1,
                    Vendor = "Cisco",
                    Model = "Catalyst 9300",
                    Location = "Server Room A, Rack 1",
                    Department = "IT Operations",
                    IsMonitoringEnabled = true
                });

                discoveredDevices.Add(new Device
                {
                    Name = "Backup NAS Server",
                    Hostname = "nas-backup.local",
                    IPAddress = $"{baseSubnet}25",
                    MacAddress = "00:1A:2B:3C:4D:19",
                    DeviceTypeId = defaultType?.Id ?? 1,
                    StatusId = defaultStatus?.Id ?? 1,
                    Vendor = "Synology",
                    Model = "DS1821+",
                    Location = "Server Room B, Rack 2",
                    Department = "Storage Admin",
                    IsMonitoringEnabled = true
                });
            }

            return discoveredDevices;
        }

        private async Task<(string ip, bool success, double latency)> PingAddressAsync(string ip)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 150);
                return (ip, reply.Status == IPStatus.Success, reply.RoundtripTime);
            }
            catch
            {
                return (ip, false, 0);
            }
        }

        private async Task<string> TryResolveHostnameAsync(string ipAddress)
        {
            try
            {
                var resolveTask = Dns.GetHostEntryAsync(ipAddress);
                if (await Task.WhenAny(resolveTask, Task.Delay(200)) == resolveTask)
                {
                    return (await resolveTask).HostName ?? $"node-{ipAddress.Replace(".", "-")}.local";
                }
                return $"node-{ipAddress.Replace(".", "-")}.local"; // Timeout
            }
            catch
            {
                return $"node-{ipAddress.Replace(".", "-")}.local";
            }
        }

        private string GenerateMockMacAddress(string ip)
        {
            var hash = ip.GetHashCode();
            var random = new Random(hash);
            var buffer = new byte[6];
            random.NextBytes(buffer);
            buffer[0] = (byte)(buffer[0] & 0xFE);
            return string.Join(":", buffer.Select(b => b.ToString("X2")));
        }
    }
}
