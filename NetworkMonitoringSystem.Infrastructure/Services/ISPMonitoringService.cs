using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Services
{
    /// <summary>
    /// Background Hangfire job that pings each ISP's gateway targets,
    /// measures latency and packet loss, and updates the ISP status.
    /// </summary>
    public class ISPMonitoringService : IISPMonitoringService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ISPMonitoringService> _logger;

        // Latency thresholds (ms)
        private const double DegradedThresholdMs = 120.0;
        private const double CriticalThresholdMs = 400.0;

        // Packet loss thresholds (%)
        private const double DegradedLossPercent  = 10.0;
        private const double OfflineLossPercent   = 70.0;

        public ISPMonitoringService(
            ApplicationDbContext db,
            ILogger<ISPMonitoringService> logger)
        {
            _db     = db;
            _logger = logger;
        }

        public async Task ProcessISPChecksAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[ISP Monitor] Starting ISP connectivity checks...");

            var isps = await _db.ISPs
                .Where(i => i.IsMonitoringEnabled)
                .ToListAsync(cancellationToken);

            if (!isps.Any())
            {
                _logger.LogInformation("[ISP Monitor] No ISPs configured for monitoring.");
                return;
            }

            foreach (var isp in isps)
            {
                try
                {
                    // Probe primary target (5 pings for loss %)
                    var (avgMs, lossPercent) = await PingWithStatsAsync(isp.PingTarget, count: 5);

                    // If primary shows loss, cross-check against secondary
                    if (lossPercent >= OfflineLossPercent && !string.IsNullOrWhiteSpace(isp.SecondaryPingTarget))
                    {
                        var (secAvgMs, secLoss) = await PingWithStatsAsync(isp.SecondaryPingTarget, count: 3);
                        // Average the two targets
                        avgMs      = (avgMs + secAvgMs) / 2.0;
                        lossPercent = (lossPercent + secLoss) / 2.0;
                    }

                    string previousStatus = isp.Status;
                    string newStatus;

                    if (lossPercent >= OfflineLossPercent || avgMs <= 0)
                    {
                        newStatus = "Offline";
                        isp.DowntimeCount++;
                        isp.TotalDowntimeMinutes += 1; // job runs every minute
                    }
                    else if (lossPercent >= DegradedLossPercent || avgMs >= DegradedThresholdMs)
                    {
                        newStatus = "Degraded";
                    }
                    else
                    {
                        newStatus = "Online";
                    }

                    isp.Status             = newStatus;
                    isp.LastLatencyMs      = Math.Round(avgMs, 1);
                    isp.PacketLossPercent  = Math.Round(lossPercent, 1);
                    isp.LastCheckedAt      = DateTime.UtcNow;

                    if (previousStatus != newStatus)
                    {
                        if (newStatus == "Offline")
                            _logger.LogError("[ISP Monitor] ISP '{Name}' ({Provider}) is now OFFLINE. Loss={Loss:F0}%", 
                                isp.Name, isp.Provider, lossPercent);
                        else if (newStatus == "Degraded")
                            _logger.LogWarning("[ISP Monitor] ISP '{Name}' is DEGRADED. Latency={Ms:F0} ms, Loss={Loss:F1}%", 
                                isp.Name, avgMs, lossPercent);
                        else
                            _logger.LogInformation("[ISP Monitor] ISP '{Name}' returned to ONLINE. Latency={Ms:F0} ms", 
                                isp.Name, avgMs);
                    }
                    else
                    {
                        _logger.LogInformation("[ISP Monitor] {Name}: {Status} | {Ms:F0} ms | Loss {Loss:F1}%", 
                            isp.Name, newStatus, avgMs, lossPercent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ISP Monitor] Error checking ISP '{Name}'", isp.Name);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[ISP Monitor] ISP checks completed.");
        }

        // ── Helpers ────────────────────────────────────────────────────
        private async Task<(double avgMs, double lossPercent)> PingWithStatsAsync(
            string target, int count = 5)
        {
            int success = 0;
            double totalMs = 0;

            var tasks = new List<Task<(bool ok, double ms)>>();
            for (int i = 0; i < count; i++)
                tasks.Add(SinglePingAsync(target));

            var results = await Task.WhenAll(tasks);
            foreach (var r in results)
            {
                if (r.ok) { success++; totalMs += r.ms; }
            }

            double avgMs     = success > 0 ? totalMs / success : 0;
            double lossPercent = ((double)(count - success) / count) * 100.0;
            return (avgMs, lossPercent);
        }

        private async Task<(bool ok, double ms)> SinglePingAsync(string target, int timeoutMs = 2000)
        {
            try
            {
                using var ping  = new Ping();
                var reply = await ping.SendPingAsync(target, timeoutMs);
                return (reply.Status == IPStatus.Success, reply.RoundtripTime);
            }
            catch
            {
                return (false, 0);
            }
        }
    }
}
