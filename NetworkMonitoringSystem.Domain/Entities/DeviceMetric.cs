using System;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class DeviceMetric
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public virtual Device? Device { get; set; }
        public bool IsOnline { get; set; }
        public double ResponseTimeMs { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }
}
