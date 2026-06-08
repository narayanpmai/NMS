using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Domain.Entities;
using NetworkMonitoringSystem.Infrastructure.Data;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Services
{
    public class PingService : IPingService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PingService> _logger;

        public PingService(
            ApplicationDbContext dbContext,
            INotificationService notificationService,
            ILogger<PingService> logger)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessPingChecksAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Ping Monitoring Job check...");

            var devices = await _dbContext.Devices
                .Where(d => d.IsMonitoringEnabled)
                .Include(d => d.Status)
                .ToListAsync(cancellationToken);

            if (!devices.Any())
            {
                _logger.LogInformation("No active devices found for monitoring.");
                return;
            }

            var onlineStatus = await _dbContext.DeviceStatuses.FirstOrDefaultAsync(s => s.Name == "Online", cancellationToken);
            var offlineStatus = await _dbContext.DeviceStatuses.FirstOrDefaultAsync(s => s.Name == "Offline", cancellationToken);
            var warningStatus = await _dbContext.DeviceStatuses.FirstOrDefaultAsync(s => s.Name == "Warning", cancellationToken);

            if (onlineStatus == null || offlineStatus == null)
            {
                _logger.LogWarning("Device statuses not fully seeded. Skipping monitoring cycle.");
                return;
            }

            foreach (var device in devices)
            {
                try
                {
                    var (isOnline, responseTime) = await PingDeviceAsync(device.IPAddress);

                    // Simulate SNMP CPU/Memory checks
                    var random = new Random(device.Id + DateTime.UtcNow.Second);
                    var cpuUsage = isOnline ? Math.Round(10.0 + random.NextDouble() * 85.0, 1) : 0.0;
                    var memoryUsage = isOnline ? Math.Round(30.0 + random.NextDouble() * 65.0, 1) : 0.0;

                    var metric = new DeviceMetric
                    {
                        DeviceId = device.Id,
                        IsOnline = isOnline,
                        ResponseTimeMs = responseTime,
                        CpuUsage = cpuUsage,
                        MemoryUsage = memoryUsage,
                        CheckedAt = DateTime.UtcNow
                    };
                    _dbContext.DeviceMetrics.Add(metric);

                    int newStatusId;
                    string newStatusName;
                    if (!isOnline)
                    {
                        newStatusId = offlineStatus.Id;
                        newStatusName = "Offline";
                    }
                    else if (responseTime > 200 && warningStatus != null)
                    {
                        newStatusId = warningStatus.Id;
                        newStatusName = "Warning";
                    }
                    else
                    {
                        newStatusId = onlineStatus.Id;
                        newStatusName = "Online";
                    }

                    var previousStatusId = device.StatusId;
                    var oldStatusName = device.Status?.Name ?? "Unknown";

                    if (previousStatusId != newStatusId)
                    {
                        device.StatusId = newStatusId;

                        // Create alert if status transitioned specifically from Online to Offline
                        if (newStatusId == offlineStatus.Id && oldStatusName == "Online")
                        {
                            var alert = new Alert
                            {
                                DeviceId = device.Id,
                                Message = $"Device '{device.Name}' ({device.IPAddress}) transitioned from Online to Offline.",
                                Level = "Critical"
                            };
                            _dbContext.Alerts.Add(alert);

                            // Log error as requested
                            _logger.LogError("ALERT: Device {DeviceName} ({IP}) went OFFLINE from Online.", device.Name, device.IPAddress);

                            // Auto-create Incident Ticket
                            var incident = new Incident
                            {
                                Title = $"Downtime: {device.Name} is Offline",
                                Description = alert.Message,
                                Severity = "Critical",
                                Status = "Open",
                                DeviceId = device.Id,
                                Alert = alert
                            };
                            _dbContext.Incidents.Add(incident);

                            await _notificationService.SendAlertAsync(device.Name, alert.Message, alert.Level);
                        }
                        // Auto-resolve alerts and incidents if device goes from Offline to Online
                        else if (newStatusId == onlineStatus.Id && oldStatusName == "Offline")
                        {
                            var unresolvedAlerts = await _dbContext.Alerts
                                .Where(a => a.DeviceId == device.Id && !a.IsResolved)
                                .ToListAsync(cancellationToken);
                            foreach (var a in unresolvedAlerts)
                            {
                                a.IsResolved = true;
                                a.ResolvedAt = DateTime.UtcNow;
                            }

                            var unresolvedIncidents = await _dbContext.Incidents
                                .Where(i => i.DeviceId == device.Id && i.Title.StartsWith("Downtime:") && i.Status != "Resolved" && i.Status != "Closed")
                                .ToListAsync(cancellationToken);
                            foreach (var inc in unresolvedIncidents)
                            {
                                inc.Status = "Resolved";
                                inc.ResolvedAt = DateTime.UtcNow;
                                inc.RootCause = "Device returned online.";
                            }

                            _logger.LogInformation("Device {DeviceName} ({IP}) is back ONLINE. Resolved {Count} alerts and {IncCount} incidents.", device.Name, device.IPAddress, unresolvedAlerts.Count, unresolvedIncidents.Count);
                        }

                        _logger.LogInformation("Device {DeviceName} status changed: {Old} -> {New}", device.Name, oldStatusName, newStatusName);
                    }

                    // Evaluate active alert rules thresholds
                    await EvaluateAlertRulesAsync(device, metric, cancellationToken);

                    // Push live status updates
                    await _notificationService.SendStatusUpdateAsync(device.Id, device.Name, newStatusName, responseTime, metric.CpuUsage, metric.MemoryUsage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error pinging device {DeviceName} ({IP}).", device.Name, device.IPAddress);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Push updated dashboard summary totals
            var totalDevices = devices.Count;
            var onlineCount = devices.Count(d => d.StatusId == onlineStatus.Id);
            var warningCount = warningStatus != null ? devices.Count(d => d.StatusId == warningStatus.Id) : 0;
            var offlineCount = devices.Count(d => d.StatusId == offlineStatus.Id);

            await _notificationService.SendDashboardUpdateAsync(totalDevices, onlineCount, warningCount, offlineCount);
            _logger.LogInformation("Ping monitoring job completed successfully.");
        }

        private async Task<(bool isOnline, double responseTimeMs)> PingDeviceAsync(string ipAddress)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, 3000);
                return (reply.Status == IPStatus.Success, reply.RoundtripTime);
            }
            catch
            {
                return (false, 0);
            }
        }

        private async Task EvaluateAlertRulesAsync(Device device, DeviceMetric metric, CancellationToken cancellationToken)
        {
            var activeRules = await _dbContext.AlertRules
                .Where(r => r.IsEnabled)
                .ToListAsync(cancellationToken);

            foreach (var rule in activeRules)
            {
                double? actualValue = null;
                if (rule.MetricType.Equals("Latency", StringComparison.OrdinalIgnoreCase) || 
                    rule.MetricType.Equals("ResponseTime", StringComparison.OrdinalIgnoreCase))
                {
                    actualValue = metric.ResponseTimeMs;
                }
                else if (rule.MetricType.Equals("CPU", StringComparison.OrdinalIgnoreCase))
                {
                    actualValue = metric.CpuUsage;
                }
                else if (rule.MetricType.Equals("Memory", StringComparison.OrdinalIgnoreCase))
                {
                    actualValue = metric.MemoryUsage;
                }

                if (actualValue.HasValue)
                {
                    var isBreached = EvaluateThreshold(actualValue.Value, rule.Operator, rule.ThresholdValue);

                    var activeIncident = await _dbContext.Incidents
                        .Include(i => i.Alert)
                        .FirstOrDefaultAsync(i => i.DeviceId == device.Id && 
                                                  i.Title.Contains(rule.Name) && 
                                                  i.Status != "Resolved" && 
                                                  i.Status != "Closed", 
                                             cancellationToken);

                    if (isBreached)
                    {
                        if (activeIncident == null)
                        {
                            var alert = new Alert
                            {
                                DeviceId = device.Id,
                                Message = $"Alert Rule '{rule.Name}' breached for device '{device.Name}'. Actual: {actualValue.Value:F1} ms, Threshold: {rule.ThresholdValue:F1} ms",
                                Level = rule.Severity,
                                IsResolved = false
                            };
                            _dbContext.Alerts.Add(alert);

                            _logger.LogError("ALERT BREACH: {Message}", alert.Message);

                            var incident = new Incident
                            {
                                Title = $"Threshold Breached: {rule.Name} on {device.Name}",
                                Description = alert.Message,
                                Severity = rule.Severity,
                                Status = "Open",
                                DeviceId = device.Id,
                                Alert = alert
                            };
                            _dbContext.Incidents.Add(incident);

                            await _notificationService.SendAlertAsync(device.Name, alert.Message, alert.Level);
                        }
                    }
                    else
                    {
                        if (activeIncident != null)
                        {
                            activeIncident.Status = "Resolved";
                            activeIncident.ResolvedAt = DateTime.UtcNow;
                            activeIncident.RootCause = "Metric returned within safe thresholds.";

                            if (activeIncident.Alert != null)
                            {
                                activeIncident.Alert.IsResolved = true;
                                activeIncident.Alert.ResolvedAt = DateTime.UtcNow;
                            }

                            _logger.LogInformation("ALERT RECOVERED: Rule {RuleName} on device {DeviceName} returned to normal levels.", rule.Name, device.Name);
                        }
                    }
                }
            }
        }

        private bool EvaluateThreshold(double actualValue, string op, double thresholdValue)
        {
            return op switch
            {
                ">" => actualValue > thresholdValue,
                "<" => actualValue < thresholdValue,
                "==" => Math.Abs(actualValue - thresholdValue) < 0.001,
                ">=" => actualValue >= thresholdValue,
                "<=" => actualValue <= thresholdValue,
                _ => false
            };
        }

        public async Task CalculateMonthlySlaAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Calculating monthly SLA records for all devices...");
            
            var devices = await _dbContext.Devices.ToListAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            foreach (var device in devices)
            {
                var metrics = await _dbContext.DeviceMetrics
                    .Where(m => m.DeviceId == device.Id && m.CheckedAt >= firstDayOfMonth)
                    .ToListAsync(cancellationToken);

                if (!metrics.Any())
                {
                    _logger.LogInformation("No metrics found for device '{DeviceName}' in the current month. Skipping SLA record.", device.Name);
                    continue;
                }

                var onlineCount = metrics.Count(m => m.IsOnline);
                var uptimePercentage = ((double)onlineCount / metrics.Count) * 100.0;

                var record = await _dbContext.SlaRecords
                    .FirstOrDefaultAsync(r => r.DeviceId == device.Id && r.Month == firstDayOfMonth, cancellationToken);

                if (record == null)
                {
                    record = new SLARecord
                    {
                        DeviceId = device.Id,
                        Month = firstDayOfMonth,
                        UptimePercentage = Math.Round(uptimePercentage, 2),
                        TargetPercentage = 99.9
                    };
                    _dbContext.SlaRecords.Add(record);
                }
                else
                {
                    record.UptimePercentage = Math.Round(uptimePercentage, 2);
                    _dbContext.SlaRecords.Update(record);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Monthly SLA calculations completed successfully.");
        }
    }
}
