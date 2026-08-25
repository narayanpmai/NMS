using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Services
{
    public class SnmpService : ISnmpService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SnmpService> _logger;

        public SnmpService(ApplicationDbContext context, ILogger<SnmpService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task PollDevicesAsync()
        {
            var devices = await _context.Devices
                .Where(d => d.IsMonitoringEnabled && !string.IsNullOrEmpty(d.SnmpCommunity))
                .ToListAsync();

            foreach (var device in devices)
            {
                try
                {
                    if (!IPAddress.TryParse(device.IPAddress, out var ip))
                        continue;

                    var community = new OctetString(device.SnmpCommunity);
                    var sysDescrOid = new ObjectIdentifier("1.3.6.1.2.1.1.1.0"); // SNMPv2-MIB::sysDescr.0

                    // Use Task.Run as Messenger.Get is synchronous and block thread
                    var result = await Task.Run(() => Messenger.Get(
                        VersionCode.V2,
                        new IPEndPoint(ip, device.SnmpPort),
                        community,
                        new List<Variable> { new Variable(sysDescrOid) },
                        2000)); // 2s timeout

                    if (result != null && result.Count > 0)
                    {
                        var sysDescr = result[0].Data.ToString();
                        _logger.LogInformation("SNMP Poll Success for {IP}: {SysDescr}", device.IPAddress, sysDescr);
                        
                        // Update device model with SNMP sysDescr info as proof of polling
                        device.Model = sysDescr.Length > 200 ? sysDescr.Substring(0, 200) : sysDescr;
                        _context.Update(device);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("SNMP Poll failed for {IP}: {Error}", device.IPAddress, ex.Message);
                }
            }
            
            await _context.SaveChangesAsync();
        }
    }
}
